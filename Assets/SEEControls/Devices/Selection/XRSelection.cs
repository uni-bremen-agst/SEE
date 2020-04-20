using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Devices
{
    public class XRSelection : Selection
    {
        [Tooltip("The threshold at which the trigger is considered to be activated."), Range(0.01f, 1.0f)]
        public float Threshold = 0.1f;

        [Tooltip("The VR controller for pointing")]
        public Hand PointingHand;

        private SteamVR_Action_Single TriggerAction = SteamVR_Input.GetSingleAction(defaultActionSet, "Trigger");

        public override Vector3 Value
        {
            get => SteamVR_Actions.default_Pose.GetLocalRotation(PointingHand.handType) * Vector3.forward;
        }

        public override bool Activated
        {
            get => TriggerAction != null ? TriggerAction.axis >= Threshold : false;
        }
    }
}