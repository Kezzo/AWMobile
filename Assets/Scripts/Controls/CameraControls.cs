using System;
using UnityEngine;

/// <summary>
/// Controls the camera movement.
/// </summary>
public class CameraControls : MonoBehaviour
{
    [SerializeField]
    private CameraType m_cameraType;

    [SerializeField]
    [Range(1f, 10f)]
    private float m_rotationSpeed;

    [SerializeField]
    [Range(0f, 1f)]
    private float m_scrollSpeed;

    [SerializeField]
    private Camera m_cameraToControl;
    public Camera CameraToControl { get { return m_cameraToControl; } }

    [SerializeField]
    private Transform m_cameraMover;

    private Vector3 m_lastMousePosition;
    private bool m_draggingTop;

    private bool m_startedDragging;
    public bool IsDragging { get; private set; }

    public enum CameraType
    {
        Rotate,
        Scroll
    }

    private Action m_cameraUpdateAction;

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);

        switch (m_cameraType)
        {
            case CameraType.Rotate:
                m_cameraUpdateAction = HandleRotationCamera;
                break;
            case CameraType.Scroll:
                m_cameraUpdateAction = HandleScrollCamera;
                break;
        }
    }

    /// <summary>
    /// Focuses the camera on the middle of the battlefield.
    /// </summary>
    private void Start()
    {
        if (m_cameraType == CameraType.Rotate)
        {
            CameraLookAtWorldCenter();
        }
    }

    /// <summary>
    /// Listens to the mouse button input (touch also works) and handles a selected camera movement type.
    /// </summary>
    private void Update ()
    {
        m_cameraUpdateAction();
    }

    /// <summary>
    /// Scrolls the camera when dragging on the screen.
    /// </summary>
    private void HandleScrollCamera()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = m_lastMousePosition - Input.mousePosition;

            if (m_startedDragging)
            {
                if (!IsDragging && (Mathf.Abs(mouseDelta.x) > 1f || Mathf.Abs(mouseDelta.y) > 1f))
                {
                    IsDragging = true;
                }

                float yPosition = m_cameraMover.position.y;

                m_cameraMover.localPosition += new Vector3(mouseDelta.x * m_scrollSpeed, mouseDelta.y * m_scrollSpeed, 0f);

                m_cameraMover.position = new Vector3(m_cameraMover.position.x, yPosition, m_cameraMover.position.z);
            }

            m_lastMousePosition = Input.mousePosition;
            m_startedDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            //Debug.Log("Stopped Dragging");

            IsDragging = false;
            m_startedDragging = false;
            m_lastMousePosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Rotates the cameras root when dragging on the screen
    /// </summary>
    private void HandleRotationCamera()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = m_lastMousePosition - Input.mousePosition;

            if (m_startedDragging)
            {
                float rotationChangeX = mouseDelta.x * m_rotationSpeed * Time.deltaTime;
                float rotationChangeY = mouseDelta.y * m_rotationSpeed * Time.deltaTime;

                //Debug.LogFormat("Rotation Change X: '{0}' Y: '{1}'", rotationChangeX, rotationChangeY);

                if (!IsDragging && (Mathf.Abs(rotationChangeX) > 0.1f || Mathf.Abs(rotationChangeX) > 0.1f))
                {
                    //Debug.Log("Started Dragging");
                    IsDragging = true;
                }

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
            m_startedDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            //Debug.Log("Stopped Dragging");

            IsDragging = false;
            m_startedDragging = false;
            m_lastMousePosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Focuses the camera on the middle of the world.
    /// </summary>
    public void CameraLookAtWorldCenter()
    {
        m_cameraToControl.transform.LookAt(new Vector3(0, 0, 0));
    }
}
