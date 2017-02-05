#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionControls : MonoBehaviour
{
    [SerializeField]
    private Camera m_battlegroundCamera;

    [SerializeField]
    private LayerMask m_unitLayerMask;

    [SerializeField]
    private LayerMask m_movementFieldLayerMask;

    [SerializeField]
    private LayerMask m_attackFieldLayerMask;

    [SerializeField]
    private LayerMask m_moveDestinationFieldLayerMask;

    [SerializeField]
    private CameraControls m_cameraControls;

    private BaseUnit m_currentlySelectedUnit;
    private bool m_abortNextSelectionTry;

    private List<Vector2> m_routeToDestinationField;

    private Dictionary<Vector2, PathfindingNodeDebugData> m_pathfindingNodeDebug;

    public bool IsBlocked { get; set; }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (!Root.Instance.DebugValues.m_ShowPathfindingDebugData || m_routeToDestinationField == null ||
                m_pathfindingNodeDebug == null)
            {
                return;
            }

            foreach (var node in m_pathfindingNodeDebug)
            {
                //Debug.Log(node);

                BaseMapTile baseMapTileOnPath = ControllerContainer.TileNavigationController.GetMapTile(node.Key);

                if (baseMapTileOnPath != null)
                {
                    // Draw sphere
                    Handles.color = m_routeToDestinationField.Contains(node.Key) ? Color.red : Color.gray;
                    Handles.SphereCap(0, baseMapTileOnPath.transform.position + Vector3.up, Quaternion.identity, 0.5f);

                    // Draw text label
                    GUIStyle guiStyle = new GUIStyle
                    {
                        normal = {textColor = Color.black},
                        alignment = TextAnchor.MiddleCenter
                    };

                    Handles.Label(baseMapTileOnPath.transform.position + Vector3.up + Vector3.back,
                        string.Format("C{0} P{1}", node.Value.CostToMoveToNode, node.Value.NodePriority), guiStyle);
                }
            }
        }
    }
#endif

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
    }

    private void Start()
    {
        ControllerContainer.BattleController.AddTurnStartEvent("DeselectUnit", teamPlayingNext => DeselectCurrentUnit()); 
    }

    // Update is called once per frame
    private void Update ()
    {
        if (IsBlocked)
        {
            return;
        }

        if (Input.GetMouseButton(0) && m_cameraControls.IsDragging)
        {
            m_abortNextSelectionTry = true;
            //Debug.Log("Aborting Next Selection Try");
        }

        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        if (ControllerContainer.BattleController.IsPlayersTurn() && !m_abortNextSelectionTry)
        {
            RaycastHit raycastHit;

            if (m_currentlySelectedUnit != null && TrySelection(m_battlegroundCamera, m_attackFieldLayerMask, out raycastHit))
            {
                Debug.Log("Selected attack field");

                // Select attack field
                StartUnitAttack(raycastHit);
            }
            else if (TrySelection(m_battlegroundCamera, m_unitLayerMask, out raycastHit))
            {
                Debug.Log("Selected unit");

                // Select Unit
                SelectUnit(raycastHit);
            }
            else if (m_currentlySelectedUnit != null &&
                     TrySelection(m_battlegroundCamera, m_movementFieldLayerMask, out raycastHit))
            {
                Debug.Log("Selected movement field");

                // Select movement field
                CalculateRouteToMovementField(raycastHit);

            }
            else if (m_currentlySelectedUnit != null &&
                     TrySelection(m_battlegroundCamera, m_moveDestinationFieldLayerMask, out raycastHit))
            {
                Debug.Log("Selected destination movement field");

                // confirm move
                ControllerContainer.BattleController.OnConfirmMove();
            }
            else if (m_currentlySelectedUnit != null && !IsPointerOverUIObject())
            {
                // Deselect unit
                DeselectCurrentUnit();
                Debug.Log("Deselected Unit");
            }
        }
        else if (m_abortNextSelectionTry)
        {
            m_abortNextSelectionTry = false;

            //Debug.Log("Selection Try was aborted!");
        }
    }

    /// <summary>
    /// Starts the attack of the currently selected unit.
    /// </summary>
    /// <param name="raycastHit">The raycast hit.</param>
    private void StartUnitAttack(RaycastHit raycastHit)
    {
        BaseUnit unitToAttack = raycastHit.transform.parent.parent.GetComponent<BaseUnit>();

        if (unitToAttack != null)
        {
            m_currentlySelectedUnit.AttackUnit(unitToAttack);
            DeselectCurrentUnit();
        }
    }

    /// <summary>
    /// Selects a unit.
    /// </summary>
    /// <param name="raycastHit">The raycast hit.</param>
    private void SelectUnit(RaycastHit raycastHit)
    {
        BaseUnit selectedUnit = raycastHit.transform.GetComponent<BaseUnit>();

        if (selectedUnit != null && selectedUnit.CanUnitTakeAction())
        {
            DeselectCurrentUnit();

            m_currentlySelectedUnit = selectedUnit;
            m_currentlySelectedUnit.OnUnitWasSelected();
        }
    }

    /// <summary>
    /// Calculates the route to a movement field.
    /// </summary>
    /// <param name="raycastHit">The raycast hit.</param>
    private void CalculateRouteToMovementField(RaycastHit raycastHit)
    {
        BaseMapTile baseMapTile = raycastHit.transform.parent.parent.GetComponent<BaseMapTile>();

        if (baseMapTile != null)
        {
            m_routeToDestinationField = ControllerContainer.TileNavigationController.
                GetBestWayToDestination(m_currentlySelectedUnit, baseMapTile, out m_pathfindingNodeDebug);
            m_currentlySelectedUnit.DisplayRouteToDestination(m_routeToDestinationField, DeselectCurrentUnit);
        }
    }

    /// <summary>
    /// De-Selects the current unit.
    /// The input parameter unitident exists to be able to only de-select the current unit, if it has the given unit ident.
    /// This is for example needed for the case where a unit is moving and when it reaches its destination would call this method.
    /// If the user selects another unit in the meantime, we can make sure that the current unit is not de-selected because it has a different unit ident.
    /// </summary>
    private void DeselectCurrentUnit(int unitIdent = -1)
    {
        if (m_currentlySelectedUnit == null || unitIdent >= 0 && m_currentlySelectedUnit.UniqueIdent != unitIdent)
        {
            return;
        }

        m_currentlySelectedUnit.OnUnitWasDeselected();
        m_currentlySelectedUnit = null;
    }

    /// <summary>
    /// Tries selecting a unit
    /// </summary>
    /// <param name="cameraToBaseSelectionOn">The camera to base selection on.</param>
    /// <param name="selectionMask">The selection mask.</param>
    /// <param name="selectionTarget">The selection target.</param>
    /// <returns></returns>
    private bool TrySelection(Camera cameraToBaseSelectionOn, LayerMask selectionMask, out RaycastHit selectionTarget)
    {
        Ray selectionRay = cameraToBaseSelectionOn.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));

        //To support orthographic cameras
        if (cameraToBaseSelectionOn.orthographic)
        {
            selectionRay.direction = cameraToBaseSelectionOn.transform.forward.normalized;
        }

        Debug.DrawRay(selectionRay.origin, selectionRay.direction * 100f, Color.yellow, 1f);

        return Physics.Raycast(selectionRay, out selectionTarget, 100f, selectionMask);
    }

    /// <summary>
    /// Determines whether a pointer is over an UI object.
    /// This is the only solution that also works on mobile.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if [is pointer over UI object]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
