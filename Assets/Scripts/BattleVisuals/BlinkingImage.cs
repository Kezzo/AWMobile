using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BlinkingImage : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField]
    private float m_alphaValueChangePerTimestep;

    private Image m_imageToBlink;
    private bool m_increasingAlpha;

    private void Start()
    {
        m_imageToBlink = GetComponent<Image>();
    }

	// Update is called once per frame
	private void Update ()
	{
	    float newAlphaValue = m_imageToBlink.color.a;
	    newAlphaValue += m_increasingAlpha ? m_alphaValueChangePerTimestep : -m_alphaValueChangePerTimestep;

        m_imageToBlink.color = new Color(m_imageToBlink.color.r, m_imageToBlink.color.g, m_imageToBlink.color.b, newAlphaValue);

	    if (m_imageToBlink.color.a > 0.99f)
	    {
	        m_increasingAlpha = false;
	    }
        else if (m_imageToBlink.color.a < 0.01f)
	    {
	        m_increasingAlpha = true;
	    }
    }
}
