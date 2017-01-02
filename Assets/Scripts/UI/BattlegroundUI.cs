using UnityEngine;

public class BattlegroundUI : MonoBehaviour
{
    [SerializeField]
    private GameObject m_confirmMoveButton;

    [SerializeField]
    private GameObject m_endTurnButton;

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
    }

    /// <summary>
    /// Changes the visibility of the confirm move button.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    public void ChangeVisibilityOfConfirmMoveButton(bool setVisible)
    {
        m_confirmMoveButton.SetActive(setVisible);
    }

    /// <summary>
    /// Called when the confirm move button was pressed.
    /// </summary>
    public void OnConfirmMoveButtonPressed()
    {
        ControllerContainer.BattleController.OnConfirmMove();
    }

    /// <summary>
    /// Changes the visibility of end turn button.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    public void ChangeVisibilityOfEndTurnButton(bool setVisible)
    {
        m_endTurnButton.SetActive(setVisible);
    }

    /// <summary>
    /// Called when the end turn button was pressed.
    /// </summary>
    public void OnEndTurnButtonPressed()
    {
        ControllerContainer.BattleController.EndCurrentTurn();
    }

    /// <summary>
    /// Tests a button.
    /// </summary>
    public void TestButton()
    {
        Debug.Log("Button works!");
    }
}
