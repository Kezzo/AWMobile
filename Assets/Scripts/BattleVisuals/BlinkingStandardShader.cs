using UnityEngine;

public class BlinkingStandardShader : MonoBehaviour
{
    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private Color m_firstColor;

    [SerializeField]
    private Color m_secondColor;

    [Range(0f, 3f)]
    [SerializeField]
    private float m_blinkTime;

    private float m_colorClampValue;
    private bool decreasingValue;

    private void OnEnable()
    {
        m_colorClampValue = 0f;
        decreasingValue = false;
        m_renderer.material.color = m_firstColor;
    }

    // Update is called once per frame
    private void Update ()
    {
        if (decreasingValue)
        {
            m_colorClampValue -= m_blinkTime * Time.deltaTime;

            if (m_colorClampValue < 0f)
            {
                decreasingValue = false;
            }
        }
        else
        {
            m_colorClampValue += m_blinkTime * Time.deltaTime;

            if (m_colorClampValue > 1f)
            {
                decreasingValue = true;
            }
        }

        m_colorClampValue = Mathf.Clamp(m_colorClampValue, 0f, 1f);

        //Debug.Log(m_colorClampValue);

        m_renderer.material.color = Color.Lerp(m_firstColor, m_secondColor, m_colorClampValue);
    }
}
