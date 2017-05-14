using System.Collections;
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

    [Range(0f, 5f)]
    [SerializeField]
    private float m_blinkCooldown;

    private void OnEnable()
    {
        m_renderer.material.SetFloat("_Alpha", m_alphaMax);
        m_renderer.SetPropertyBlock(new MaterialPropertyBlock());

        StartCoroutine(BlinkWithCooldown());
    }

    /// <summary>
    /// Triggers the BlinkOnce coroutine and waits for the defined blink cooldown as long as the gameobject is enabled.
    /// </summary>
    private IEnumerator BlinkWithCooldown()
    {
        while (true)
        {
            yield return new WaitForSeconds(m_blinkCooldown);

            yield return StartCoroutine(BlinkOnce());
        }
    }

    /// <summary>
    /// Fades shader, this component is attached to, out once and fades it in again.
    /// </summary>
    private IEnumerator BlinkOnce()
    {
        float alphaClampValue = m_alphaMax;
        bool decreasingValue = true;

        while (true)
        {
            if (decreasingValue)
            {
                alphaClampValue -= m_blinkTime * Time.deltaTime;

                if (alphaClampValue <= m_alphaMin)
                {
                    decreasingValue = false;
                }
            }
            else
            {
                alphaClampValue += m_blinkTime * Time.deltaTime;

                if (alphaClampValue >= m_alphaMax)
                {
                    yield break;
                }
            }

            alphaClampValue = Mathf.Clamp(alphaClampValue, m_alphaMin, m_alphaMax);

            m_renderer.material.SetFloat("_Alpha", alphaClampValue);

            yield return null;
        }
    }
}
