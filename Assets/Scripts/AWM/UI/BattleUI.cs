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
        private BattleResultUI m_battleResultUi;

        [SerializeField]
        private BattlePauseUI m_battlePauseUi;

        [SerializeField]
        private TitleUI m_titleUi;

        private void Awake()
        {
            CC.MBR.Register(this);

            if (Root.Instance.SceneLoading.IsInLevelSelection)
            {
                ChangeVisibilityOfBattleUI(false);
            }
            else
            {
                CC.BSC.OnBattleStartListener.Add("BattleUI - Initialize", Initialize);
                CC.BSC.OnTeamWonListener.Add("BattleUI - ShowBattleEndVisuals", ShowBattleEndVisuals);

                CC.BSC.OnTeamDoneThisTurnListener.Add("BattleUI - ChangeEndTurnButtonHighlightState",
                    teamColor =>
                    {
                        if (CC.BSC.IsTeamWithColorPlayersTeam(teamColor))
                        {
                            ChangeEndTurnButtonHighlightState(true);
                        }
                    });

                CC.BSC.OnTurnStartListener.Add("BattleUI - ChangeEndTurnButtonHighlightState",
                    team =>
                    {
                        if (!CC.BSC.IsTeamWithColorPlayersTeam(team.m_TeamColor))
                        {
                            ChangeEndTurnButtonHighlightState(false);
                        }
                    });

                m_battlePauseUi.SetVisibilityCallback(ChangeVisibilityOfBattleUI);
            }
        }

        private void Start()
        {
            if (Root.Instance.SceneLoading.IsInLevelSelection && !Root.Instance.HasShownTitleUI)
            {
                m_titleUi.Show();
                Root.Instance.HasShownTitleUI = true;
            }
        }

        /// <summary>
        /// Initializes the specified teams this battle.
        /// </summary>
        /// <param name="teamsThisBattle">The teams this battle.</param>
        private void Initialize(Team[] teamsThisBattle)
        {
            //TODO: Display team stats and show battle introduction etc.
            CC.BSC.OnTurnStartListener.Add("BattleUI - Initialize", teamToStartNext =>
                m_endTurnButton.SetActive(teamToStartNext.m_IsPlayersTeam));
        }

        /// <summary>
        /// Changes the visibility of end turn button.
        /// </summary>
        /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
        public void ChangeVisibilityOfBattleUI(bool setVisible)
        {
            m_pauseButton.SetActive(setVisible);

            // to ensure the end turn button is not displayed in an enemy turn.
            if (setVisible && !CC.BSC.IsPlayersTurn())
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
            CC.InputBlocker.ChangeBattleControlInput(true);
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
            CC.BSC.EndCurrentTurn();
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
