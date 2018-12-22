using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    public class TitleUI : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        private bool loadingIntoFirstLevel = false;

        public void Show()
        {
            animator.SetBool("Visible", true);
            Root.Instance.IsInputBlocked = true;
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
                Root.Instance.IsInputBlocked = false;
            });
        }
    }
}