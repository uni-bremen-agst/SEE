using InControl;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Input from a touchpad. 
    /// </summary>
    public class TouchDevice : InputDevice
    {
        private void Update()
        {
            // Use last device which provided input.
            InControl.InputDevice inputDevice = InputManager.ActiveDevice;

            // Disable and hide touch controls if we use a controller.
            // If "Enable Controls On Touch" is ticked in Touch Manager inspector,
            // controls will be enabled and shown again when the screen is touched.
            if (inputDevice != InControl.InputDevice.Null && inputDevice != TouchManager.Device)
            {
                TouchManager.ControlsEnabled = false;
            }

            /// inputDevice.LeftStick gives us only a Vector2.
            /// The direction of the movement in 3D is as follows:
            /// x => left/right, y => up/down, z => forward/backward
            /// We will use only left/right and forward/backward for the movement.
            movementDirection.Invoke(new Vector3(inputDevice.LeftStick.Value.x, 0, inputDevice.LeftStick.Value.y));
            pointingDirection.Invoke(inputDevice.RightStick.Value);
        }
    }
}
