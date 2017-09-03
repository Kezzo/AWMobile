using System;
using UnityEngine;

/// <summary>
/// Class to handle UI initialization and interaction in the Battleground
/// </summary>
public class BattlegroundUI : MonoBehaviour
{
    [SerializeField]
    private GameObject m_confirmMoveButton;

    [SerializeField]
    private GameObject m_endTurnButton;

    [SerializeField]
    private BattleSequenceUIElement m_battleSequenceUiElement;

    [SerializeField]
    private BattleResultUI m_battleResultUi;

    private InputBlocker m_inputBlocker;

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
        ControllerContainer.BattleController.AddBattleStartedEvent("BattleGroundUI - Initialize", Initialize);
        ControllerContainer.BattleController.AddBattleEndedEvent("BattleGroundUI - ShowBattleEndVisuals", ShowBattleEndVisuals);

        m_inputBlocker = new InputBlocker();
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
    /// Shows a battle sequence.
    /// </summary>
    /// <param name="leftMapTileData">The left map tile data.</param>
    /// <param name="healthOfLeftUnit">The health of left unit.</param>
    /// <param name="rightMapTileData">The right map tile data.</param>
    /// <param name="healthOfRightUnit">The health of right unit.</param>
    /// <param name="damageToRightUnit">The damage to right unit.</param>
    /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
    public void ShowBattleSequence(MapGenerationData.MapTile leftMapTileData, int healthOfLeftUnit, 
        MapGenerationData.MapTile rightMapTileData, int healthOfRightUnit, int damageToRightUnit, Action onBattleSequenceFinished)
    {
        bool activateEndTurnButton = m_endTurnButton.activeSelf;

        ChangeVisibilityOfEndTurnButton(false);

        m_battleSequenceUiElement.InitializeAndStartSequence(leftMapTileData, healthOfLeftUnit, 
            rightMapTileData, healthOfRightUnit, damageToRightUnit, () =>
            {
                if (onBattleSequenceFinished != null)
                {
                    onBattleSequenceFinished();
                }

                if (activateEndTurnButton)
                {
                    ChangeVisibilityOfEndTurnButton(true);
                }
            });
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
    public void ChangeVisibilityOfEndTurnButton(bool setVisible)
    {
        m_endTurnButton.SetActive(setVisible);
    }

    /// <summary>
    /// Changes the visibility of the battle-end visuals.
    /// </summary>
    /// <param name="teamColorThatWon">The teamcolor of the team that won.</param>
    private void ShowBattleEndVisuals(TeamColor teamColorThatWon)
    {
        m_inputBlocker.ChangeBattleControlInput(true);
        ChangeVisibilityOfEndTurnButton(false);

        m_battleResultUi.Show(teamColorThatWon);
        // TODO: Improve visuals of ui
        // TODO: Restart game when ok/retry button is pressed.
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
