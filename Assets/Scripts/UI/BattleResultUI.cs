using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the visibility of the BattleResultUI.
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    [SerializeField]
    private GameObject m_gameEndVisuals;

    [SerializeField]
    private Text m_battleResultText;

    [SerializeField]
    private Animator m_animator;

    private InputBlocker m_inputBlocker;

    /// <summary>
    /// Shows this UI.
    /// </summary>
    /// <param name="teamThatWon">The team that won.</param>
    public void Show(TeamColor teamThatWon)
    {
        m_gameEndVisuals.SetActive(true);
        m_battleResultText.text = string.Format("Team {0} won the match!", teamThatWon);
        m_animator.SetTrigger("Show");
    }

    /// <summary>
    /// Called when the ok button was pressed.
    /// </summary>
    public void OnOkButtonPressed()
    {
        if (m_inputBlocker == null)
        {
            m_inputBlocker = new InputBlocker();
        }

        m_inputBlocker.ChangeBattleControlInput(true);

        Root.Instance.LoadingUi.Show();

        Root.Instance.CoroutineHelper.CallDelayed(Root.Instance, 1.05f, () =>
        {
            Root.Instance.SceneLoading.UnloadExistingScenes(() =>
            {
                Root.Instance.SceneLoading.LoadToLevelSelection(() =>
                {
                    m_inputBlocker.ChangeBattleControlInput(false);
                    Root.Instance.LoadingUi.Hide();
                });
            });
        });
    }
}
