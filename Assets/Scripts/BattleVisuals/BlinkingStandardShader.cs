using UnityEngine;

public class BlinkingStandardShader : MonoBehaviour
{
    [SerializeField]
    private Renderer m_renderer;

    [SerializeField]
    private float m_alphaMax;

    [SerializeField]
    private float m_alphaMin;

    [Range(0f, 5f)]
    [SerializeField]
    private float m_blinkTime;

    private float m_alphaClampValue;
    private bool decreasingValue;

    private void OnEnable()
    {
        m_alphaClampValue = m_alphaMax;
        decreasingValue = false;
        m_renderer.material.SetFloat("_Alpha", m_alphaMax);
        m_renderer.SetPropertyBlock(new MaterialPropertyBlock());
    }

    // Update is called once per frame
    private void Update ()
    {
        if (decreasingValue)
        {
            m_alphaClampValue -= m_blinkTime * Time.deltaTime;

            if (m_alphaClampValue <= m_alphaMin)
            {
                decreasingValue = false;
            }
        }
        else
        {
            m_alphaClampValue += m_blinkTime * Time.deltaTime;

            if (m_alphaClampValue >= m_alphaMax)
            {
                decreasingValue = true;
            }
        }

        m_alphaClampValue = Mathf.Clamp(m_alphaClampValue, m_alphaMin, m_alphaMax);

        m_renderer.material.SetFloat("_Alpha", m_alphaClampValue);
    }
}
