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

        [Tooltip("The least amount of seconds the selection and grab buttons must have been pressed to be considered activated."), 
                  Range(0.01f, 1.0f)]
        public const float ButtonDurationThreshold = 0.5f;

        [Tooltip("The threshold at which the trigger is considered to be activated."), Range(0.01f, 1.0f)]
        public float Threshold = 0.1f;

        [Tooltip("The VR controller for pointing")]
        public Hand PointingHand;

        private SteamVR_Action_Vector2 PullAction = SteamVR_Input.GetVector2Action(defaultActionSet, "Move");

        //private SteamVR_Action_Single TriggerAction = SteamVR_Input.GetSingleAction(defaultActionSet, "Trigger");

        /// <summary>
        /// The default assignment of the grab button in SteamVR is the B button,
        /// but it may be re-assigned by the user.
        /// </summary>
        private SteamVR_Action_Boolean GrabButton = SteamVR_Input.GetBooleanAction(defaultActionSet, "Grab");

        /// <summary>
        /// The default assignment of the selection button in SteamVR is the A button,
        /// but it may be re-assigned by the user.
        /// </summary>
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
        /// The current toggle value of the select button ("Select" in SteamVR).
        /// </summary>
        public override bool IsSelecting => selectionButton.State;        

        /// <summary>
        /// True if the user presses the grabbing button ("Grab" in SteamVR) long enough.
        /// </summary>
        public override bool IsGrabbing => grabButton.TransientState;

        /// <summary>
        /// The degree of the trigger (the SteamVR axis assigned as "Trigger").
        /// </summary>
        public override float Pull
        {
            get
            {
                float move = PullAction.axis.y;
                return Mathf.Abs(move) >= Threshold ? -move : 0.0f;
            }
        }

        /// <summary>
        /// Manages the behaviour of SelectionButton.
        /// </summary>
        private DelayedToggle selectionButton;

        /// <summary>
        /// Manages the behaviour of GrabButton.
        /// </summary>
        private DelayedToggle grabButton;

        private class DelayedToggle
        {
            /// <summary>
            /// Whether this toggle has been pressed long enough. The selection works as a toggle
            /// that can be turned on and off. Each mode continues until the button is pressed again.
            /// </summary>
            public bool State => state;

            /// <summary>
            /// Returns the state and if the button is currently pressed, it resets the state to false.
            /// </summary>
            public bool TransientState
            {
                get
                {
                    bool result = state;
                    if (state)
                    {
                        state = false;
                        buttonEventConsumed = false;
                    }
                    return result;
                }
            }
            /// <summary>
            /// Current state: true for down and false for up.
            /// </summary>
            private bool state = false;

            /// <summary>
            /// Indicates whether the change of the button has already triggered
            /// the wanted behaviour, that is, lead to toggling the state.
            /// A user could press the button longer than the necessary time,
            /// in which case we want to maintain the state rather than wobbling
            /// its value. If this value is false, continuing pressing the button
            /// will be ignored until the button is released again.
            /// </summary>
            private bool buttonEventConsumed = false;

            /// <summary>
            /// The SteamVR button from which to get the true raw input.
            /// </summary>
            private SteamVR_Action_Boolean button;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="button">the SteamVR button from which to get the user input</param>
            public DelayedToggle(SteamVR_Action_Boolean button)
            {
                this.button = button;
            }

            public void OnUpdate()
            {
                // button.state is true if the button is currently pressed.
                // button.stateDown is true if the button was not pressed and is now being pressed.
                //    It indicates whether the user has just started to press the button.
                // button.stateUp is true if the button was pressed and is now being released.
                //    It indicates whether the user has just released a pressed button.
                // button.lastChanged is true if there is a change of the state, i.e.
                //    it is equivalent to button.stateDown || button.stateUp.
                // button.updateTime is the point in real-time where the button was queried last.
                // button.changedTime is the point in real-time where button.lastChanged was true last.
                // Time.time - button.changedTime gives us the duration since the last change. 
                if (button.stateDown)
                {
                    buttonEventConsumed = false;
                }
                else if (button.stateUp)
                {
                    buttonEventConsumed = true;
                }
                else if (button.state)
                {
                    if (!buttonEventConsumed && Time.realtimeSinceStartup - button.changedTime >= ButtonDurationThreshold)
                    {
                        state = !state;
                        buttonEventConsumed = true;
                    }
                }
            }
        }

        private void Start()
        {
            selectionButton = new DelayedToggle(SelectionButton);
            grabButton = new DelayedToggle(GrabButton);
        }

        private void Update()
        {
            selectionButton.OnUpdate();
            grabButton.OnUpdate();
        }
    }
}