﻿using System;
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
            Screen.SetResolution((int) (Screen.width * 0.7f), (int) (Screen.height * 0.7f), true);

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
                    CoroutineHelper.CallDelayed(this, 0.1f, () =>
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
            ControllerContainer.UnitBalancingProvider.InitializeBalancingData();
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