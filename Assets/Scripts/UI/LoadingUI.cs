using UnityEngine;

/// <summary>
/// Controls the visibility of the LoadingUI.
/// </summary>
public class LoadingUI : MonoBehaviour
{
    [SerializeField]
    private Animator m_animator;

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
        m_animator.SetTrigger("Show");
    }

    /// <summary>
    /// Hides this instance.
    /// </summary>
    public void Hide()
    {
        m_animator.SetTrigger("Hide");
    }
}
