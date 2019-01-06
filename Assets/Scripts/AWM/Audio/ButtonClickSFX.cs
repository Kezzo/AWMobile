using AWM.System;
using UnityEngine;
using UnityEngine.UI;

namespace AWM.Audio
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSFX : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => Root.Instance.SFXManager.PlaySFX(SoundEffect.ButtonClick));
        }
    }
}