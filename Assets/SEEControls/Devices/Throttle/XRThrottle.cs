using Valve.VR;

namespace SEE.Controls.Devices
{
    public class XRThrottle : Throttle
    {       
        private SteamVR_Action_Single ThrottleAction = SteamVR_Input.GetSingleAction(defaultActionSet, ThrottleActionName);

        public override float Value
        {
            get => ThrottleAction != null ? ThrottleAction.axis : 0;
        }
    }
}