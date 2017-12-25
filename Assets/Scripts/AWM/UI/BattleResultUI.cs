using AWM.Enums;
using AWM.System;
using TMPro;
using UnityEngine;

namespace AWM.UI
{
    /// <summary>
    /// Controls the visibility of the BattleResultUI.
    /// </summary>
    public class BattleResultUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_gameEndVisuals;

        [SerializeField]
        private TextMeshProUGUI m_battleResultText;

        [SerializeField]
        private Animator m_animator;

        /// <summary>
        /// Shows this UI.
        /// </summary>
        /// <param name="teamThatWon">The team that won.</param>
        public void Show(TeamColor teamThatWon)
        {
            m_gameEndVisuals.SetActive(true);
            m_battleResultText.text = ControllerContainer.BattleStateController.IsTeamWithColorPlayersTeam(teamThatWon) ?
                "You won the match!" :
                "The enemy team won the match!";

            m_animator.SetTrigger("Show");
        }

        /// <summary>
        /// Called when the ok button was pressed.
        /// </summary>
        public void OnOkButtonPressed()
        {
            ControllerContainer.InputBlocker.ChangeBattleControlInput(true);

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
}
