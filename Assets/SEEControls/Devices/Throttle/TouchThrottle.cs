using InControl;

namespace SEE.Controls.Devices
{
    public class TouchThrottle : Throttle
    { 
        public override float Value
        {
            get => InputManager.ActiveDevice.RightTrigger.Value;
        }
    }
}