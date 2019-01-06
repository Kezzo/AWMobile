using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    public class TitleUI : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        private bool loadingIntoFirstLevel = false;
        LevelSelectionUI levelSelectionUI;

        public void Show(LevelSelectionUI levelSelectionUI)
        {
            this.levelSelectionUI = levelSelectionUI;
            Root.Instance.IsInputBlocked = true;

            Root.Instance.CoroutineHelper.CallDelayed(this, 1f, () =>
            {
                animator.SetBool("Visible", true);
            });
        }

        public void StartGame()
        {
            if (!CC.PPS.HasBeatenFirstLevel)
            {
                if (loadingIntoFirstLevel)
                {
                    return;
                }

                loadingIntoFirstLevel = true;

                Root.Instance.LoadingUi.Show();

                // 0.5 second delay needed here to have a smooth animation.
                Root.Instance.CoroutineHelper.CallDelayed(this, 1.05f, () =>
                {
                    Root.Instance.SceneLoading.UnloadExistingScenes(() =>
                    {
                        Root.Instance.SceneLoading.LoadToLevel("Level1", () =>
                        {
                            Root.Instance.LoadingUi.Hide();
                            CC.PPS.LastPlayedLevel = "Level1";
                            Root.Instance.IsInputBlocked = false;
                        });

                    });
                });

                return;
            }

            animator.SetBool("Visible", false);

            Root.Instance.CoroutineHelper.CallDelayed(this, 0.2f, () =>
            {
                levelSelectionUI.ChangeVisibilityOfHeader(true);
                Root.Instance.IsInputBlocked = false;
            });
        }
    }
}