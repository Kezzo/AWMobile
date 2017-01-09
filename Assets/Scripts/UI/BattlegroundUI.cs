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
        ControllerContainer.BattleController.AddBattleStartedEvent("BattleGroundUI - Initialize", Initialize);
    }

    /// <summary>
    /// Initializes the specified teams this battle.
    /// </summary>
    /// <param name="teamsThisBattle">The teams this battle.</param>
    private void Initialize(Team[] teamsThisBattle)
    {
        //TODO: Display team stats and show battle introduction etc.
        ControllerContainer.BattleController.AddTurnStartEvent("BattleGroundUI - Initialize", teamToStartNext => 
            ChangeVisibilityOfEndTurnButton(teamToStartNext.m_IsPlayersTeam));
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
    /// Changes the visibility of end turn button.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    private void ChangeVisibilityOfEndTurnButton(bool setVisible)
    {
        m_endTurnButton.SetActive(setVisible);
    }

    /// <summary>
    /// Called when the confirm move button was pressed.
    /// </summary>
    public void OnConfirmMoveButtonPressed()
    {
        ControllerContainer.BattleController.OnConfirmMove();
    }

    /// <summary>
    /// Called when the end turn button was pressed.
    /// </summary>
    public void OnEndTurnButtonPressed()
    {
        ControllerContainer.BattleController.EndCurrentTurn();
    }
}
