using InControl;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A throttle device based on a gamepad controller or touch screen based on InControl.
    /// </summary>
    public class TouchGamepadThrottle : Throttle
    { 
        /// <summary>
        /// Is 1 if the left trigger of the active device was pressed and
        /// 0 otherwise.
        /// </summary>
        public override float Value
        {
            get => InputManager.ActiveDevice.LeftTrigger.Value;
        }
    }
}