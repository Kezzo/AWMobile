﻿using System;
using AWM.System;
using TMPro;
using UnityEngine;

namespace AWM.UI
{
    public class BattlePauseUI : MonoBehaviour
    {
        [SerializeField]
        private Animator m_animator;

        [SerializeField]
        private TextMeshProUGUI m_endMatchText;

        private Action<bool> m_onVisibilityChange;
        private bool endMatchWasPressed = false;
        private bool paused = false;

        private void Awake()
        {
            m_endMatchText.text = !CC.PPS.HasBeatenFirstLevel ? "RESTART" : "END MATCH";
        }

        /// <summary>
        /// Sets a visibility callback that is invokes when the pause ui is shown or hidden.
        /// </summary>
        /// <param name="onVisibilityChange">The on visibility change.</param>
        public void SetVisibilityCallback(Action<bool> onVisibilityChange)
        {
            m_onVisibilityChange = onVisibilityChange;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape) && !Root.Instance.SceneLoading.IsInLevelSelection)
            {
                if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Hidden"))
                {
                    Show();
                }
                else if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Visible"))
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// Shows this UI.
        /// </summary>
        public void Show()
        {
            if(paused)
            {
                return;
            }

            paused = true;

            Root.Instance.AudioManager.ToggleSfxPause(true);

            CC.InputBlocker.ChangeBattleControlInput(true);
            CC.BSC.IsBattlePaused = true;

            if (m_onVisibilityChange != null)
            {
                m_onVisibilityChange(false);
            }

            m_animator.SetBool("Visible", true);
        }

        /// <summary>
        /// Hides this ui.
        /// </summary>
        public void Hide()
        {
            if (!paused)
            {
                return;
            }

            paused = false;

            Root.Instance.AudioManager.ToggleSfxPause(false);

            m_animator.SetBool("Visible", false);
            CC.InputBlocker.ChangeBattleControlInput(false);
            CC.BSC.IsBattlePaused = false;

            if (m_onVisibilityChange != null)
            {
                m_onVisibilityChange(true);
            }
        }

        /// <summary>
        /// Called when the back button pressed.
        /// </summary>
        public void OnBackButtonPressed()
        {
            if(endMatchWasPressed)
            {
                return;
            }

            endMatchWasPressed = true;

            Root.Instance.LoadingUi.Show();
            Root.Instance.AudioManager.sfxIsPaused = false;

            Root.Instance.CoroutineHelper.CallDelayed(Root.Instance, 1.05f, () =>
            {
                Root.Instance.SceneLoading.UnloadExistingScenes(() =>
                {
                    if (!CC.PPS.HasBeatenFirstLevel)
                    {
                        Root.Instance.SceneLoading.LoadToLevel("Level1", () =>
                        {
                            CC.InputBlocker.ChangeBattleControlInput(false);
                            Root.Instance.LoadingUi.Hide();
                        });
                    }
                    else
                    {
                        Root.Instance.SceneLoading.LoadToLevelSelection(() =>
                        {
                            CC.InputBlocker.ChangeBattleControlInput(false);
                            Root.Instance.LoadingUi.Hide();
                        });
                    }

                    
                });
            });
        }
    }
}
