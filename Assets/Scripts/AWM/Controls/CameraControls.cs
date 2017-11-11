using System.Collections;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.Controls
{
    /// <summary>
    /// Controls the camera movement.
    /// </summary>
    public class CameraControls : MonoBehaviour
    {
        [SerializeField]
        [Range(1f, 10f)]
        private float m_rotationSpeed;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_scrollSpeed;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_scrollSpeedOrthographic;

        [SerializeField]
        [Range(0.01f, 0.2f)]
        private float m_zoomSpeed;

        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float m_maxZoomLevel;

        [SerializeField]
        [Range(-10.0f, 0.0f)]
        private float m_minZoomLevel;

        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float m_maxZoomLevelOrthographic;

        [SerializeField]
        [Range(0.0f, 20.0f)]
        private float m_minZoomLevelOrthographic;

        [SerializeField]
        private Camera m_cameraToControl;
        public Camera CameraToControl { get { return m_cameraToControl; } }

        [SerializeField]
        private Camera m_secondayCameraToControl;
        public Camera SecondaryCameraToControl { get { return m_secondayCameraToControl; } }

        [SerializeField]
        private Transform m_cameraMover;

        [Header("Auto-Zoom")]

        [SerializeField]
        [Range(0f, 200f)]
        private float m_autoZoomSpeed;

        [SerializeField]
        private AnimationCurve m_autoZoomAnimationCurve;

        private Vector2 m_lastMousePosition;
        private float m_cameraStartPosZ;

        private float m_zoomLevel;
        public float CurrentZoomLevel { get { return m_zoomLevel; } }

        private bool m_isManuallyScrolling;
        private bool m_startedScrolling;

        private bool m_isManuallyZooming;

        public bool IsMovingCamera { get { return m_isManuallyScrolling || m_isManuallyZooming; } }

        public bool IsBlocked { get; set; }

        private bool m_autoZoomIsRunning;
        private float m_zoomLevelInPlayerTurn;
        private bool m_lastTurnWasPlayerTurn;

        private Coroutine m_lastRunningAutoZoomCoroutine;

        private int m_fingerIdUsedToScroll = -1;

        private void Awake()
        {
            ControllerContainer.MonoBehaviourRegistry.Register(this);
        }

        /// <summary>
        /// Focuses the camera on the middle of the battlefield.
        /// </summary>
        private void Start()
        {
            m_cameraStartPosZ = m_cameraToControl.transform.localPosition.z;
            m_secondayCameraToControl.transform.localPosition = m_cameraToControl.transform.localPosition;

            ControllerContainer.BattleController.AddTurnStartEvent("CameraAutoZoom", ZoomCameraBasedOnTheCurrentTeam);

            m_zoomLevel = CameraToControl.orthographic ? CameraToControl.orthographicSize : 0f;
        }

        /// <summary>
        /// Listens to the mouse button input (touch also works) and handles a selected camera movement type.
        /// </summary>
        private void LateUpdate()
        {
            if (IsBlocked)
            {
                m_isManuallyScrolling = false;
                m_startedScrolling = false;

                m_isManuallyZooming = false;

                m_lastMousePosition = Vector3.zero;

                return;
            }

            ScrollCamera();
            ZoomCamera();
        }

        /// <summary>
        /// Scrolls the camera when dragging on the screen.
        /// </summary>
        private void ScrollCamera()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor && 
                Input.GetMouseButtonDown(0))
            {
                m_fingerIdUsedToScroll = Input.GetTouch(0).fingerId;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 mousePosition = GetInputPosition();
                Vector2 mouseDelta = m_lastMousePosition - mousePosition;

                if (Application.platform != RuntimePlatform.WindowsEditor && 
                    Input.GetTouch(0).fingerId != m_fingerIdUsedToScroll)
                {
                    CheckUsedScrollFinger(ref mousePosition, ref mouseDelta);
                }

                if (m_startedScrolling)
                {
                    if (!m_isManuallyScrolling && (Mathf.Abs(mouseDelta.x) > 0.1f || Mathf.Abs(mouseDelta.y) > 0.1f))
                    {
                        m_isManuallyScrolling = true;
                    }

                    float yPosition = m_cameraMover.position.y;

                    float scrollSpeed = (CameraToControl.orthographic ? m_scrollSpeedOrthographic : m_scrollSpeed) * Mathf.Clamp(Mathf.InverseLerp(
                        CameraToControl.orthographic ? m_minZoomLevelOrthographic : m_minZoomLevel,
                        CameraToControl.orthographic ? m_maxZoomLevelOrthographic : m_maxZoomLevel, m_zoomLevel), 0.3f, 1f);

                    m_cameraMover.localPosition += new Vector3(mouseDelta.x * scrollSpeed, mouseDelta.y * scrollSpeed, 0f);
                    m_cameraMover.position = new Vector3(m_cameraMover.position.x, yPosition, m_cameraMover.position.z);
                }

                m_lastMousePosition = mousePosition;
                m_startedScrolling = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_isManuallyScrolling = false;
                m_startedScrolling = false;
                m_lastMousePosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Checks the currently used scroll finger.
        /// If touch fingers touch the screen and input from a different then the one that started the scrolling was received,
        /// the correct scrolling will be used to set the mouseposition.
        /// If the scroll finger was raised and a new finger is the scroll finger, the registered finger is updated.
        /// </summary>
        private void CheckUsedScrollFinger(ref Vector2 mousePosition, ref Vector2 mouseDelta)
        {
            if (Input.touchCount > 1)
            {
                for (int i = 0; i < Input.touches.Length; i++)
                {
                    if (Input.touches[i].fingerId == m_fingerIdUsedToScroll)
                    {
                        mouseDelta = m_lastMousePosition - Input.touches[i].position;
                        mousePosition = Input.touches[i].position;
                    }
                }
            }
            else
            {
                mouseDelta = Vector2.zero;
                m_fingerIdUsedToScroll = Input.GetTouch(0).fingerId;
            }
        }

        /// <summary>
        /// Gets the input position based on the platform the app is running and the amount of touches.
        /// </summary>
        /// <returns></returns>
        private Vector2 GetInputPosition()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Input.mousePosition;
            }
            else
            {
                return Input.GetTouch(0).position;
            }
        }

        /// <summary>
        /// Handles the zoom pinch.
        /// </summary>
        private void ZoomCamera()
        {
            float deltaMagnitudeDifference = 0f;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                deltaMagnitudeDifference = -(Input.mouseScrollDelta.y * 20f);
            }
            else
            {
                if (Input.touchCount == 2)
                {
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    Vector2 touchZeroPreviousPosition = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePreviousPosition = touchOne.position - touchOne.deltaPosition;

                    float previousTouchDeltaMagnitude = (touchZeroPreviousPosition - touchOnePreviousPosition).magnitude;
                    float touchDeltaMagnitude = (touchZero.position - touchOne.position).magnitude;

                    deltaMagnitudeDifference = previousTouchDeltaMagnitude - touchDeltaMagnitude;
                }
            }

            if (Mathf.Abs(deltaMagnitudeDifference) > 0.1f)
            {
                StopRunningAutoZoom();
                ChangeZoomLevel(deltaMagnitudeDifference, m_cameraToControl);
                ChangeZoomLevel(deltaMagnitudeDifference, m_secondayCameraToControl);

                m_isManuallyZooming = true;
            }
            else
            {
                m_isManuallyZooming = false;
            }
        }

        /// <summary>
        /// Zooms the camera based on the currently playing team.
        /// </summary>
        private void ZoomCameraBasedOnTheCurrentTeam(Team currentlyPlayingTeam)
        {
            if (!currentlyPlayingTeam.m_IsPlayersTeam && m_lastTurnWasPlayerTurn)
            {
                if (!m_autoZoomIsRunning)
                {
                    m_zoomLevelInPlayerTurn = CurrentZoomLevel;
                }
            
                AutoZoom(10f);

                m_lastTurnWasPlayerTurn = false;
            }
            else if(currentlyPlayingTeam.m_IsPlayersTeam)
            {
                //TODO: Handle multiple enemy teams
                AutoZoom(m_zoomLevelInPlayerTurn);

                m_lastTurnWasPlayerTurn = true;
            }
        }

        /// <summary>
        /// Automatically zooms to a given zoom level.
        /// </summary>
        /// <param name="zoomLevel">The zoom level.</param>
        /// <returns></returns>
        private void AutoZoom(float zoomLevel)
        {
            StopRunningAutoZoom();

            m_lastRunningAutoZoomCoroutine = StartCoroutine(AutoZoomCoroutine(zoomLevel));
        }

        /// <summary>
        /// Stops the currently running auto zoom coroutine.
        /// </summary>
        private void StopRunningAutoZoom()
        {
            if (m_lastRunningAutoZoomCoroutine != null)
            {
                StopCoroutine(m_lastRunningAutoZoomCoroutine);
                m_lastRunningAutoZoomCoroutine = null;

                m_autoZoomIsRunning = false;
            }
        }

        /// <summary>
        /// Automatically zooms to a given zoom level.
        /// </summary>
        /// <param name="zoomLevel">The zoom level.</param>
        /// <returns></returns>
        private IEnumerator AutoZoomCoroutine(float zoomLevel)
        {
            float startZoomLevel = m_zoomLevel;

            if (Mathf.Abs(startZoomLevel - zoomLevel) < 0.1f)
            {
                m_autoZoomIsRunning = false;
                yield break;
            }
            m_autoZoomIsRunning = true;

            bool zoomingOut = zoomLevel > startZoomLevel;

            while (true)
            {
                float normalizedZoomedLength = (m_zoomLevel - startZoomLevel) / (zoomLevel - startZoomLevel);

                float zoomLevelChangeThisFrame = m_autoZoomSpeed * m_autoZoomAnimationCurve.Evaluate(normalizedZoomedLength) * Time.deltaTime;

                zoomLevelChangeThisFrame = zoomingOut ? zoomLevelChangeThisFrame : zoomLevelChangeThisFrame * -1;
                zoomLevelChangeThisFrame = Mathf.Clamp(zoomLevelChangeThisFrame, m_minZoomLevel, m_maxZoomLevel);

                ChangeZoomLevel(zoomLevelChangeThisFrame, m_cameraToControl);
                ChangeZoomLevel(zoomLevelChangeThisFrame, m_secondayCameraToControl);

                if (Mathf.Abs(m_zoomLevel - zoomLevel) <= 0.1f)
                {
                    m_autoZoomIsRunning = false;
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Changes the zoom level of a given camera.
        /// </summary>
        /// <param name="zoomDelta">The zoom delta.</param>
        /// <param name="cameraToZoom">The camera to zoom.</param>
        private void ChangeZoomLevel(float zoomDelta, Camera cameraToZoom)
        {
            if (cameraToZoom.orthographic)
            {
                cameraToZoom.orthographicSize += zoomDelta * m_zoomSpeed;
                cameraToZoom.orthographicSize = Mathf.Clamp(cameraToZoom.orthographicSize, m_minZoomLevelOrthographic, m_maxZoomLevelOrthographic);
                m_zoomLevel = cameraToZoom.orthographicSize;
            }
            else
            {
                m_zoomLevel = Mathf.Clamp(m_zoomLevel + zoomDelta * m_zoomSpeed, m_minZoomLevel, m_maxZoomLevel);

                cameraToZoom.transform.localPosition = new Vector3(cameraToZoom.transform.localPosition.x,
                    cameraToZoom.transform.localPosition.y, m_cameraStartPosZ - m_zoomLevel);
            }
        }

        /// <summary>
        /// Sets the position of the camera mover to the given vector.
        /// </summary>
        /// <param name="cameraPosition">The position to set the camera to.</param>
        public void SetCameraPositionTo(Vector3 cameraPosition)
        {
            m_cameraMover.localPosition = cameraPosition;
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
    }
}