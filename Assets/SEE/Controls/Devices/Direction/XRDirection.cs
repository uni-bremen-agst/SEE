using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Yields directions for movements retrieved from a virtual-reality controller.
    /// </summary>
    public class XRDirection : Direction
    {
        [Tooltip("The threshold at which the throttle is considered to be activated."), Range(0.01f, 1.0f)]
        public float Threshold = 0.1f;

        [Tooltip("The VR controller for directing")]
        public Hand DirectingHand;

        /// <summary>
        /// Yields the 3D direction from the rotation of the DirectingHand.
        /// </summary>
        public override Vector3 Value
        {
            get => SteamVR_Actions.default_Pose.GetLocalRotation(DirectingHand.handType) * Vector3.forward;
        }
    }
}