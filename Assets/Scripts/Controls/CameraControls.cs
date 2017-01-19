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
    private Vector2 m_touchPositionLastFrame;
    private float m_cameraStartPosZ;
    private float m_zoomLevel;

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

        m_cameraStartPosZ = m_cameraToControl.transform.localPosition.z;
    }

    /// <summary>
    /// Listens to the mouse button input (touch also works) and handles a selected camera movement type.
    /// </summary>
    private void Update()
    {
        m_cameraUpdateAction();
        HandleZoomPinch();
    }

    /// <summary>
    /// Scrolls the camera when dragging on the screen.
    /// </summary>
    private void HandleScrollCamera()
    {
#if UNITY_EDITOR
        if (Input.touchCount < 1 && Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = m_lastMousePosition - Input.mousePosition;

            if (m_startedDragging)
            {
                if (!IsDragging && (Mathf.Abs(mouseDelta.x) > 0.1f || Mathf.Abs(mouseDelta.y) > 0.1f))
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
            m_startedDragging = false;
            IsDragging = false;
            m_lastMousePosition = Vector3.zero;
        }
#endif
        if (Input.touchCount == 1)
        {
            Touch theTouch = Input.GetTouch(0);
            if (theTouch.phase == TouchPhase.Began)
            {
                m_touchPositionLastFrame = theTouch.position;
            }
            else if (theTouch.phase == TouchPhase.Moved)
            {
                Vector2 touchDelta = m_touchPositionLastFrame - theTouch.position;
                IsDragging = true;
                float yPosition = m_cameraMover.position.y;

                m_cameraMover.localPosition += new Vector3(touchDelta.x * m_scrollSpeed, touchDelta.y * m_scrollSpeed, 0f);
                m_cameraMover.position = new Vector3(m_cameraMover.position.x, yPosition, m_cameraMover.position.z);
                m_touchPositionLastFrame = theTouch.position;
            }
            else if (theTouch.phase == TouchPhase.Ended)
            {
                IsDragging = false;
                m_touchPositionLastFrame = Vector2.zero;
            }
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
    /// Handles the zoom pinch.
    /// </summary>
    private void HandleZoomPinch()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPreviousPosition = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePreviousPosition = touchOne.position - touchOne.deltaPosition;

            float previousTouchDeltaMagnitude = (touchZeroPreviousPosition - touchOnePreviousPosition).magnitude;
            float touchDeltaMagnitude = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDifference = previousTouchDeltaMagnitude - touchDeltaMagnitude;
            if (m_cameraToControl.orthographic)
            {
                m_cameraToControl.orthographicSize += deltaMagnitudeDifference;
                m_cameraToControl.orthographicSize = Mathf.Clamp(m_cameraToControl.orthographicSize, .5f, 15.0f);
            }
            else
            {
                m_zoomLevel = Mathf.Clamp(m_zoomLevel + deltaMagnitudeDifference * .05f, -10.0f, 15.0f);
                m_cameraToControl.transform.localPosition = new Vector3(CameraToControl.transform.localPosition.x, CameraToControl.transform.localPosition.y, m_cameraStartPosZ - m_zoomLevel);
            }
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
