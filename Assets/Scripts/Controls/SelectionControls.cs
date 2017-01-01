#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;

public class SelectionControls : MonoBehaviour
{
    [SerializeField]
    private Camera m_battlegroundCamera;

    [SerializeField]
    private LayerMask m_unitLayerMask;

    [SerializeField]
    private LayerMask m_movementfieldLayerMask;

    [SerializeField]
    private CameraControls m_cameraControls;

    private BaseUnit m_currentlySelectedUnit;
    private bool m_abortNextSelectionTry;

    private List<Vector2> m_routeToDestinationField;

    private Dictionary<Vector2, PathfindingNodeDebugData> m_pathfindingNodeDebug;

    private BattlegroundUI m_battlegroundUi;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (m_routeToDestinationField == null || m_pathfindingNodeDebug == null)
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
                GUIStyle guiStyle = new GUIStyle { normal = { textColor = Color.black }, alignment = TextAnchor.MiddleCenter };
                Handles.Label(baseMapTileOnPath.transform.position + Vector3.up + Vector3.back, 
                    string.Format("C{0} P{1}", node.Value.CostToMoveToNode, node.Value.NodePriority), guiStyle);
            }
        }
    }
#endif

    private void Start()
    {
        ControllerContainer.MonoBehaviourRegistry.TryGet(out m_battlegroundUi);
    }

    // Update is called once per frame
    private void Update ()
    {
        if (Input.GetMouseButton(0) && m_cameraControls.IsDragging)
        {
            m_abortNextSelectionTry = true;
            //Debug.Log("Aborting Next Selection Try");
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (ControllerContainer.BattleController.IsPlayersTurn() && !m_abortNextSelectionTry)
            {
                RaycastHit raycastHit;

                if (TrySelection(m_battlegroundCamera, m_unitLayerMask, out raycastHit))
                {
                    BaseUnit selectedUnit = raycastHit.transform.GetComponent<BaseUnit>();

                    if (selectedUnit != null && selectedUnit.CanUnitTakeAction())
                    {
                        DeselectCurrentUnit();

                        m_currentlySelectedUnit = selectedUnit;
                        m_currentlySelectedUnit.OnUnitWasSelected();
                    }
                }
                else if (m_currentlySelectedUnit != null &&
                         TrySelection(m_battlegroundCamera, m_movementfieldLayerMask, out raycastHit))
                {
                    // Get Movement field
                    // Get best movement path
                    // Draw path to walk
                    // Tell Unit to Move

                    BaseMapTile baseMapTile = raycastHit.transform.parent.parent.GetComponent<BaseMapTile>();

                    if (baseMapTile != null)
                    {
                        m_routeToDestinationField = ControllerContainer.TileNavigationController.
                            GetBestWayToDestination(m_currentlySelectedUnit, baseMapTile, out m_pathfindingNodeDebug);
                        m_currentlySelectedUnit.DisplayRouteToDestination(m_routeToDestinationField);

                        m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(true);
                    }
                    
                }
                else if (m_currentlySelectedUnit != null)
                {
                    // Deselect unit
                    DeselectCurrentUnit();
                }
            }
            else if (m_abortNextSelectionTry)
            {
                m_abortNextSelectionTry = false;

                //Debug.Log("Selection Try was aborted!");
            }
        }
    }

    /// <summary>
    /// De-Selects the current unit.
    /// </summary>
    private void DeselectCurrentUnit()
    {
        if (m_currentlySelectedUnit == null)
        {
            return;
        }

        m_currentlySelectedUnit.OnUnitWasDeselected();
        m_currentlySelectedUnit = null;

        m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(false);
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
}
