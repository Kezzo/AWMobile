using UnityEngine;

public class SelectionControls : MonoBehaviour
{
    [SerializeField]
    private Camera m_battlegroundCamera;

    [SerializeField]
    private LayerMask m_unitLayerMask;

    [SerializeField]
    private LayerMask m_movementfieldLayerMask;

    private BaseUnit m_currentlySelectedUnit;

	// Update is called once per frame
	private void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit raycastHit;

            if (TrySelection(m_battlegroundCamera, m_unitLayerMask, out raycastHit))
            {
                Debug.Log("Selected: "+ raycastHit.transform.gameObject.name);
                m_currentlySelectedUnit = raycastHit.transform.GetComponent<BaseUnit>();
            }
        }
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
