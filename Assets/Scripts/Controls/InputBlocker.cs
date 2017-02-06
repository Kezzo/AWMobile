/// <summary>
/// Class to help block the input in the battle.
/// This class is able to block the camera scroll and the selection.
/// </summary>
public class InputBlocker
{
    /// <summary>
    /// Changes the battle control input.
    /// </summary>
    /// <param name="block">if set to <c>true</c> [block].</param>
    public void ChangeBattleControlInput(bool block)
    {
        CameraControls cameraControls;
        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out cameraControls))
        {
            cameraControls.IsBlocked = block;
        }

        SelectionControls selectionControls;
        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out selectionControls))
        {
            selectionControls.IsBlocked = block;
        }
    }
}
