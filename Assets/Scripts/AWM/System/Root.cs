﻿using System;
using AWM.Audio;
using AWM.DevHelper;
using AWM.UI;
using UnityEngine;

namespace AWM.System
{
    public class Root : MonoBehaviour
    {
        [SerializeField]
        private string m_initialSceneToLoad;

        [SerializeField]
        private CoroutineHelper m_coroutineHelper;
        public CoroutineHelper CoroutineHelper { get { return m_coroutineHelper; } }

        [SerializeField]
        private SceneLoadingService m_sceneLoadingService;
        public SceneLoadingService SceneLoading { get { return m_sceneLoadingService; } }

        public LoadingUI LoadingUi { get; set; }

        [SerializeField]
        private GameObject m_blackScreen;

        [SerializeField]
        private AudioManager m_audioManager;
        public AudioManager AudioManager { get { return m_audioManager; } }

        public bool HasShownTitleUI { get; set; }

        public bool IsInputBlocked { get; set; }

#if UNITY_EDITOR
        [SerializeField]
        private DebugValues m_debugValues;
        public DebugValues DebugValues { get { return m_debugValues; } }
#endif

        // Singleton
        private static Root m_instance = null;
        public static Root Instance
        {
            get { return m_instance; }
        }

        private void Awake()
        {
            Application.targetFrameRate = 30;

            Input.multiTouchEnabled = true;

            if (m_instance == null)
            {
                m_instance = this;
            }
            else if (m_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            SceneLoading.LoadSceneAsync("LoadingUI", null, () =>
            {
                LoadingUi.Show();

                Initialize(() =>
                {
                    // 0.5 second delay needed here to have a smooth animation.
                    CoroutineHelper.CallDelayed(this, 0.5f, () =>
                    {
                        m_blackScreen.gameObject.SetActive(false);
                        LoadingUi.Hide();
                    });
                });
            });
        }

        /// <summary>
        /// Initializes the game.
        /// </summary>
        /// <param name="initializationDone">Callback when the initialization is done.</param>
        private void Initialize(Action initializationDone)
        {
            CC.UBP.InitializeBalancingData();

            m_sceneLoadingService.LoadToLevelSelection(initializationDone);
        }

        /// <summary>
        /// Restarts the game.
        /// </summary>
        /// <param name="onGameRestarted">Called when the game was restarted.</param>
        public void RestartGame(Action onGameRestarted)
        {
            m_sceneLoadingService.UnloadExistingScenes(() =>
            {
                Initialize(() =>
                {
                    if (onGameRestarted != null)
                    {
                        onGameRestarted();
                    }
                });
            });
        }
    }
}
