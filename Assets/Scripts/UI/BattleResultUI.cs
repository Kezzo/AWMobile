using UnityEngine;
using UnityEngine.UI;

public class BattleResultUI : MonoBehaviour
{
    [SerializeField]
    private GameObject m_gameEndVisuals;

    [SerializeField]
    private Text m_battleResultText;

    [SerializeField]
    private Animator m_animator;

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
        //TODO: Implement restart.
    }
}
