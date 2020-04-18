using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class XRDevice : InputDevice
    {
        [Tooltip("The controller for steering")]
        public Hand SteeringHand;
        [Tooltip("The controller for pointing")]
        public Hand PointingHand;

        private void Update()
        {
            throttle.Invoke(ThrottlePressure());
            trigger.Invoke(TriggerPressure());
            movementDirection.Invoke(GetMovementDirection());
            pointingDirection.Invoke(GetPointingDirection());
        }

        public const string defaultActionSet = "default";

        private SteamVR_Action_Single ThrottleAction = SteamVR_Input.GetSingleAction(defaultActionSet, "Throttle");
        private SteamVR_Action_Single TriggerAction = SteamVR_Input.GetSingleAction(defaultActionSet, "Trigger");

        private float ThrottlePressure()
        {
            return ThrottleAction != null ? ThrottleAction.axis : 0;
        }

        private float TriggerPressure()
        {
            return TriggerAction != null ? TriggerAction.axis : 0;
        }

        private Vector3 GetMovementDirection()
        {
            return SteamVR_Actions.default_Pose.GetLocalRotation(SteeringHand.handType) * Vector3.forward;
        }

        private Vector3 GetPointingDirection()
        {
            return SteamVR_Actions.default_Pose.GetLocalRotation(PointingHand.handType) * Vector3.forward;
        }

    }
}