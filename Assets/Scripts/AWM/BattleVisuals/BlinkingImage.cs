using UnityEngine;
using UnityEngine.UI;

namespace AWM.BattleVisuals
{
    [RequireComponent(typeof(Image))]
    public class BlinkingImage : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField]
        private float m_alphaValueChangePerTimestep;

        private Image m_imageToBlink;
        private bool m_increasingAlpha;
        private float m_alphaValue;

        private void Start()
        {
            m_imageToBlink = GetComponent<Image>();
            m_alphaValue = m_imageToBlink.color.a;
        }

        // Update is called once per frame
        private void Update ()
        {
            m_alphaValue += m_increasingAlpha ? m_alphaValueChangePerTimestep : -m_alphaValueChangePerTimestep;

            float alphaToApply = Mathf.Round(m_alphaValue);

            m_imageToBlink.color = new Color(m_imageToBlink.color.r, m_imageToBlink.color.g, m_imageToBlink.color.b, alphaToApply);

            if (m_alphaValue > 0.99f)
            {
                m_increasingAlpha = false;
            }
            else if (m_alphaValue < 0.01f)
            {
                m_increasingAlpha = true;
            }
        }
    }
}
