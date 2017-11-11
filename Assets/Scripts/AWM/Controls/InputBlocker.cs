using AWM.Enums;
using AWM.System;

namespace AWM.Controls
{
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
        /// <param name="inputBlockMode">Determines which type of input should be blocked.</param>
        public void ChangeBattleControlInput(bool block, InputBlockMode inputBlockMode = InputBlockMode.All)
        {
            if (inputBlockMode == InputBlockMode.All || inputBlockMode == InputBlockMode.CameraOnly)
            {
                CameraControls cameraControls;
                if (ControllerContainer.MonoBehaviourRegistry.TryGet(out cameraControls))
                {
                    cameraControls.IsBlocked = block;
                }
            }

            if (inputBlockMode == InputBlockMode.All || inputBlockMode == InputBlockMode.SelectionOnly)
            {
                SelectionControls selectionControls;
                if (ControllerContainer.MonoBehaviourRegistry.TryGet(out selectionControls))
                {
                    selectionControls.IsBlocked = block;
                }
            }
        }
    }
}
