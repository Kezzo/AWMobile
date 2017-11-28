using System;
using AWM.Controls;
using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    public class BattlePauseUI : MonoBehaviour
    {
        [SerializeField]
        private Animator m_animator;

        private Action<bool> m_onVisibilityChange;

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
            ControllerContainer.InputBlocker.ChangeBattleControlInput(true);

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
            m_animator.SetBool("Visible", false);
            ControllerContainer.InputBlocker.ChangeBattleControlInput(false);

            if (m_onVisibilityChange != null)
            {
                m_onVisibilityChange(true);
            }
        }
    }
}
