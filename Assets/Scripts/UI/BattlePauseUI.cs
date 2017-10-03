using UnityEngine;

public class BattlePauseUI : MonoBehaviour
{
    [SerializeField]
    private Animator m_animator;

    /// <summary>
    /// Shows this UI.
    /// </summary>
    public void Show()
    {
        m_animator.SetBool("Visible", true);
        ControllerContainer.InputBlocker.ChangeBattleControlInput(true);
    }

    /// <summary>
    /// Hides this ui.
    /// </summary>
    public void Hide()
    {
        m_animator.SetBool("Visible", false);
        ControllerContainer.InputBlocker.ChangeBattleControlInput(false);
        ControllerContainer.MonoBehaviourRegistry.Get<BattleUI>().ChangeVisibilityOfBattleUI(true);
    }

    /// <summary>
    /// Called when the back button pressed.
    /// </summary>
    public void OnBackButtonPressed()
    {
        Root.Instance.LoadingUi.Show();

        Root.Instance.CoroutineHelper.CallDelayed(Root.Instance, 1.05f, () =>
        {
            Root.Instance.SceneLoading.UnloadExistingScenes(() =>
            {
                Root.Instance.SceneLoading.LoadToLevelSelection(() =>
                {
                    ControllerContainer.InputBlocker.ChangeBattleControlInput(false);
                    Root.Instance.LoadingUi.Hide();
                });
            });
        });
    }
}
