using UnityEngine;

public class BattlegroundUI : MonoBehaviour
{
    [SerializeField]
    public GameObject m_confirmMoveButton;

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
    }

    /// <summary>
    /// Changes the visibility of the confirm move button.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    public void ChangeVisibilityOfConfirmMoveButton(bool setVisible)
    {
        m_confirmMoveButton.SetActive(setVisible);
    }

    /// <summary>
    /// Called when the confirm move button was pressed.
    /// </summary>
    public void OnConfirmMoveButtonPressed()
    {
        ControllerContainer.BattleController.OnConfirmMove();
    }
}
