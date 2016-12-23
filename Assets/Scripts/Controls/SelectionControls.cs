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
                    // Tell Unit to Move
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
