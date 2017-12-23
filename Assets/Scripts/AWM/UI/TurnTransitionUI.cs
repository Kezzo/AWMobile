using System.Collections;
using AWM.Models;
using AWM.System;
using TMPro;
using UnityEngine;

namespace AWM.UI
{
    /// <summary>
    /// Handles the UI that displays the turn transition animation and which team can play in this turn.
    /// </summary>
    public class TurnTransitionUI : MonoBehaviour
    {
        [SerializeField]
        private Animator m_animator;

        [SerializeField]
        private TextMeshProUGUI m_text;

        private string m_uiTextToUse;

        private void Start()
        {
            ControllerContainer.BattleController.AddBattleStartedListener("TurnTransitionUI", teams =>
            {
                if (teams.Length > 1)
                {
                    ControllerContainer.BattleController.AddTurnStartListener("TurnTransitionUI", team => StartCoroutine(OnTurnTransition(team)));
                }
            });
        }

        /// <summary>
        /// Invoked when a new turn started.
        /// Will display who can play in this turn.
        /// </summary>
        /// <param name="playingTeam">The team that is allowed to play this turn.</param>
        private IEnumerator OnTurnTransition(Team playingTeam)
        {
            // Needed to only start first turn transition when loading ui is hidden.
            while(Root.Instance.LoadingUi.Visible)
            {
                yield return null;
            }

            m_uiTextToUse = playingTeam.m_IsPlayersTeam ? "Your Turn!"
                : string.Format("Team {0}'s Turn!", playingTeam.m_TeamColor);
            m_animator.SetTrigger("TurnTransition");
        }

        /// <summary>
        /// Called by an animation event in the first frame of the transition animation.
        /// Needed to never update the text during an animation.
        /// </summary>
        public void UpdateText()
        {
            m_text.text = m_uiTextToUse;
        }
    }
}
