using UnityEngine;

/// <summary>
/// Controls the camera movement.
/// </summary>
public class CameraControls : MonoBehaviour
{
    [SerializeField]
    [Range(1f, 10f)]
    private float m_scrollSpeed;

    [SerializeField]
    private Transform m_cameraTransform;

    private Vector3 m_lastMousePosition;
    private bool m_draggingTop;
    private bool m_dragging;

    /// <summary>
    /// Focuses the camera on the middle of the battlefield.
    /// </summary>
    private void Start()
    {
        CameraLookAtWorldCenter();
    }

    /// <summary>
    /// Listens to the mouse button input (touch also works) and rotates the cameras root when dragging on the screen.
    /// </summary>
    private void Update ()
    {
        if (Input.GetMouseButton(0))
        {
            if (m_dragging)
            {
                Vector3 mouseDelta = m_lastMousePosition - Input.mousePosition;

                float rotationChangeX = (mouseDelta.x * m_scrollSpeed * Time.deltaTime);
                float rotationChangeY = (mouseDelta.y * m_scrollSpeed * Time.deltaTime);

                // Change rotation direction when dragging on the other side of the screen.
                if (Input.mousePosition.y < Screen.height / 2)
                {
                    rotationChangeX *= -1;
                }

                if (Input.mousePosition.x > Screen.width / 2)
                {
                    rotationChangeY *= -1;
                }

                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, rotationChangeX + rotationChangeY, 0f));
            }

            m_lastMousePosition = Input.mousePosition;
            m_dragging = true;
        }
        else if(Input.GetMouseButtonUp(0))
        {
            m_dragging = false;
        }
	}

    /// <summary>
    /// Focuses the camera on the middle of the world.
    /// </summary>
    public void CameraLookAtWorldCenter()
    {
        m_cameraTransform.LookAt(new Vector3(0, 0, 0));
    }
}
