using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A selection device using a virtual reality controller.
    /// </summary>
    public class XRSelection : Selection
    {
        [Tooltip("The threshold at which the trigger is considered to be activated."), Range(0.01f, 1.0f)]
        public float Threshold = 0.1f;

        [Tooltip("The VR controller for pointing")]
        public Hand PointingHand;

        private SteamVR_Action_Single TriggerAction = SteamVR_Input.GetSingleAction(defaultActionSet, "Trigger");

        private SteamVR_Action_Boolean GrabButton = SteamVR_Input.GetBooleanAction(defaultActionSet, "Grab");

        private SteamVR_Action_Boolean SelectionButton = SteamVR_Input.GetBooleanAction(defaultActionSet, "Select");

        /// <summary>
        /// The direction the PointingHand points to.
        /// </summary>
        public override Vector3 Direction
        {
            get => SteamVR_Actions.default_Pose.GetLocalRotation(PointingHand.handType) * Vector3.forward;
        }

        /// <summary>
        /// The position of the PointingHand.
        /// </summary>
        public override Vector3 Position
        {
            get => PointingHand.transform.position;
        }

        /// <summary>
        /// The degree of the trigger (the SteamVR axis assigned as "Trigger").
        /// </summary>
        public override float Pull => TriggerAction.axis;

        private void Update()
        {
            if (SelectionButton != null ? SelectionButton.state : false)
            {
                switch (state)
                {
                    case State.Idle:
                        state = State.IsSelecting;
                        break;
                    case State.IsSelecting:
                        state = State.IsGrabbing;
                        break;
                    case State.IsGrabbing:
                        state = State.Idle;
                        break;
                }
            }
        }
    }
}