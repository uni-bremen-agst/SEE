using Valve.VR;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A throttle device retrieving data from a virtual-reality controller 
    /// managed by SteamVR.
    /// </summary>
    public class XRThrottle : Throttle
    {       
        private SteamVR_Action_Single ThrottleAction = SteamVR_Input.GetSingleAction(defaultActionSet, ThrottleActionName);

        /// <summary>
        /// Yields the value from the "Throttle" action as assigned by the user in SteamVR.
        /// </summary>
        public override float Value
        {
            get => ThrottleAction != null ? ThrottleAction.axis : 0;
        }
    }
}