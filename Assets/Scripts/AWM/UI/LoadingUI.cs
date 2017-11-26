using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    /// <summary>
    /// Controls the visibility of the LoadingUI.
    /// </summary>
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField]
        private Animator m_animator;

        public bool Visible { get; private set; }

        /// <summary>
        /// Registers it self at the MonoBehaviourRegistry.
        /// </summary>
        private void Awake()
        {
            Root.Instance.LoadingUi = this;
        }

        /// <summary>
        /// Shows this instance.
        /// </summary>
        public void Show()
        {
            Visible = true;
            m_animator.SetTrigger("Show");
        }

        /// <summary>
        /// Hides this instance.
        /// </summary>
        public void Hide()
        {
            m_animator.SetTrigger("Hide");
        }

        /// <summary>
        /// Invoked by an animation event at the end of the hide animation.
        /// </summary>
        public void HiddenAnimationDone()
        {
            Visible = false;
        }
    }
}
