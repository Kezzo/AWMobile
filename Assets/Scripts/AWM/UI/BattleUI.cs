using System;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    /// <summary>
    /// Class to handle UI initialization and interaction in the Battleground
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_endTurnButton;

        [SerializeField]
        private Animator m_endTurnButtonAnimator;

        [SerializeField]
        private GameObject m_pauseButton;

        [SerializeField]
        private BattleSequenceUIElement m_battleSequenceUiElement;

        [SerializeField]
        private BattleResultUI m_battleResultUi;

        [SerializeField]
        private BattlePauseUI m_battlePauseUi;

        private void Awake()
        {
            ControllerContainer.MonoBehaviourRegistry.Register(this);

            if (Root.Instance.SceneLoading.IsInLevelSelection)
            {
                ChangeVisibilityOfBattleUI(false);
            }
            else
            {
                ControllerContainer.BattleStateController.OnBattleStartListener.Add("BattleUI - Initialize", Initialize);
                ControllerContainer.BattleStateController.OnTeamWonListener.Add("BattleUI - ShowBattleEndVisuals", ShowBattleEndVisuals);

                ControllerContainer.BattleStateController.OnTeamDoneThisTurnListener.Add("BattleUI - ChangeEndTurnButtonHighlightState",
                    teamColor =>
                    {
                        if (ControllerContainer.BattleStateController.IsTeamWithColorPlayersTeam(teamColor))
                        {
                            ChangeEndTurnButtonHighlightState(true);
                        }
                    });

                ControllerContainer.BattleStateController.OnTurnStartListener.Add("BattleUI - ChangeEndTurnButtonHighlightState",
                    team =>
                    {
                        if (!ControllerContainer.BattleStateController.IsTeamWithColorPlayersTeam(team.m_TeamColor))
                        {
                            ChangeEndTurnButtonHighlightState(false);
                        }
                    });

                m_battlePauseUi.SetVisibilityCallback(ChangeVisibilityOfBattleUI);
            }
        }

        /// <summary>
        /// Initializes the specified teams this battle.
        /// </summary>
        /// <param name="teamsThisBattle">The teams this battle.</param>
        private void Initialize(Team[] teamsThisBattle)
        {
            //TODO: Display team stats and show battle introduction etc.
            ControllerContainer.BattleStateController.OnTurnStartListener.Add("BattleUI - Initialize", teamToStartNext =>
                m_endTurnButton.SetActive(teamToStartNext.m_IsPlayersTeam));
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

            ChangeVisibilityOfBattleUI(false);

            m_battleSequenceUiElement.InitializeAndStartSequence(leftMapTileData, healthOfLeftUnit, 
                rightMapTileData, healthOfRightUnit, damageToRightUnit, () =>
                {
                    if (onBattleSequenceFinished != null)
                    {
                        onBattleSequenceFinished();
                    }

                    if (activateEndTurnButton)
                    {
                        ChangeVisibilityOfBattleUI(true);
                    }
                });
        }

        /// <summary>
        /// Changes the visibility of end turn button.
        /// </summary>
        /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
        public void ChangeVisibilityOfBattleUI(bool setVisible)
        {
            m_pauseButton.SetActive(setVisible);

            // to ensure the end turn button is not displayed in an enemy turn.
            if (setVisible && !ControllerContainer.BattleStateController.IsPlayersTurn())
            {
                return;
            }

            m_endTurnButton.SetActive(setVisible);
            
        }

        /// <summary>
        /// Changes the visibility of the battle-end visuals.
        /// </summary>
        /// <param name="teamColorThatWon">The teamcolor of the team that won.</param>
        private void ShowBattleEndVisuals(TeamColor teamColorThatWon)
        {
            ControllerContainer.InputBlocker.ChangeBattleControlInput(true);
            ChangeVisibilityOfBattleUI(false);

            m_battleResultUi.Show(teamColorThatWon);
            // TODO: Improve visuals of ui
        }

        /// <summary>
        /// Changes the EndTurnButton highlight state
        /// </summary>
        /// <param name="enableHighlight">if set to <c>true</c> the button highlight will be enabled; otherwise it'll be disabled.</param>
        private void ChangeEndTurnButtonHighlightState(bool enableHighlight)
        {
            m_endTurnButtonAnimator.SetBool("Highlighted", enableHighlight);
        }

        /// <summary>
        /// Called when the end turn button was pressed.
        /// </summary>
        public void OnEndTurnButtonPressed()
        {
            ControllerContainer.BattleStateController.EndCurrentTurn();
        }

        /// <summary>
        /// Called when the pause button was pressed.
        /// </summary>
        public void OnPauseButtonPressed()
        {
            m_battlePauseUi.Show();
        }
    }
}
