using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;

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
    [Range(0.05f, 0.2f)]
    private float m_zoomSpeed;

    [SerializeField]
    [Range(0.0f, 20.0f)]
    private float m_maxZoomLevel;

    [SerializeField]
    [Range(-10.0f, 0.0f)]
    private float m_minZoomLevel;

    [SerializeField]
    private Camera m_cameraToControl;
    public Camera CameraToControl { get { return m_cameraToControl; } }

    [SerializeField]
    private Transform m_cameraMover;

    [SerializeField]
    private MotionBlur m_motionBlur;

    private Vector3 m_lastMousePosition;
    private Vector2 m_touchPositionLastFrame;
    private float m_cameraStartPosZ;
    private float m_zoomLevel;
    private int m_fingersOnScreenLastFrame;

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

        m_motionBlur.enabled = IsDragging;
    }

    /// <summary>
    /// Scrolls the camera when dragging on the screen.
    /// </summary>
    private void HandleScrollCamera()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            ScrollCameraInEditor();
        }
        else
        {
            ScrollCameraOnDevice();
        }
    }

    /// <summary>
    /// Scrolls the camera on a device (Android/iOS)
    /// </summary>
    private void ScrollCameraOnDevice()
    {
        if (Input.touchCount == 1)
        {
            Touch theTouch = Input.GetTouch(0);
            if (theTouch.phase == TouchPhase.Began || m_fingersOnScreenLastFrame > 1)
            {
                m_touchPositionLastFrame = theTouch.position;
            }
            else if (theTouch.phase == TouchPhase.Moved)
            {
                Vector2 touchDelta = m_touchPositionLastFrame - theTouch.position;
                IsDragging = true;
                float yPosition = m_cameraMover.position.y;

                //Changes the speed of the camera movement, depending on zoomlevel
                float zoomScrollModifier = (m_zoomLevel - m_minZoomLevel) / (m_maxZoomLevel - m_minZoomLevel) + .3f;
                m_cameraMover.localPosition += new Vector3(touchDelta.x * m_scrollSpeed * zoomScrollModifier, touchDelta.y * m_scrollSpeed * zoomScrollModifier, 0f);
                m_cameraMover.position = new Vector3(m_cameraMover.position.x, yPosition, m_cameraMover.position.z);
                m_touchPositionLastFrame = theTouch.position;
            }
            else if (theTouch.phase == TouchPhase.Ended)
            {
                IsDragging = false;
                m_touchPositionLastFrame = Vector2.zero;
            }
        }
        m_fingersOnScreenLastFrame = Input.touchCount;
    }

    /// <summary>
    /// Scrolls the camera in the editor.
    /// </summary>
    private void ScrollCameraInEditor()
    {
        if (Input.GetMouseButton(0))
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
            ChangeZoomLevel(deltaMagnitudeDifference);
        }
    }

    /// <summary>
    /// Changes the zoom level.
    /// </summary>
    private void ChangeZoomLevel(float zoomDelta)
    {
        if (m_cameraToControl.orthographic)
        {
            m_cameraToControl.orthographicSize += zoomDelta;
            m_cameraToControl.orthographicSize = Mathf.Clamp(m_cameraToControl.orthographicSize, .5f, 15.0f);
        }
        else
        {
            m_zoomLevel = Mathf.Clamp(m_zoomLevel + zoomDelta * m_zoomSpeed, m_minZoomLevel, m_maxZoomLevel);
            m_cameraToControl.transform.localPosition = new Vector3(CameraToControl.transform.localPosition.x, CameraToControl.transform.localPosition.y, m_cameraStartPosZ - m_zoomLevel);
        }
    }


    /// <summary>
    /// Moves the Camera to look at position.
    /// </summary>
    /// <param name="targetPos">The target position.</param>
    /// <param name="time">The time.</param>
    public void CameraLookAtPosition(Vector3 targetPos, float time)
    {
        Vector3 cameraPositionWithZeroHeight = new Vector3(m_cameraMover.position.x, 0.0f, m_cameraMover.position.z);
        Ray ray = new Ray(m_cameraMover.position, m_cameraMover.forward);
        float hypotenuse = (m_cameraMover.position - cameraPositionWithZeroHeight).magnitude /
                           Mathf.Sin(m_cameraMover.parent.rotation.eulerAngles.x);
        Vector3 cameraAimTarget = ray.GetPoint(hypotenuse);
        Vector3 cameraPosition = targetPos - (m_cameraMover.position - cameraAimTarget);
        StartCoroutine(MoveCameraToPoint(cameraPosition, time));

    }

    /// <summary>
    /// This coroutine moves the camera to a point.
    /// </summary>
    /// <param name="targetPos">The target position.</param>
    /// <param name="time">The time.</param>
    /// <returns></returns>
    private IEnumerator MoveCameraToPoint(Vector3 targetPos, float time)
    {
        float timer = 0.0f;
        float timeFactor = 1 / time;

        Vector3 moverPos = m_cameraMover.position;
        while (true)
        {
            m_cameraMover.position = Vector3.Slerp(moverPos, new Vector3(targetPos.x, moverPos.y, targetPos.z), Mathf.Clamp01(timer * timeFactor));
            m_cameraMover.position = new Vector3(m_cameraMover.position.x, moverPos.y, m_cameraMover.position.z);
            timer += Time.deltaTime;
            if (timer >= time)
            {
                yield break;
            }
            yield return null;
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
