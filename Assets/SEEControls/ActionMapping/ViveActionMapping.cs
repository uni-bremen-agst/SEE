using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    [CreateAssetMenu(fileName = "ViveActionMapping", menuName = "Controls/ViveActionMapping", order = 2)]
    public class ViveActionMapping : ActionMapping
    {
        public ButtonEvent OnLeftMenuButton;
        public ButtonEvent OnRightMenuButton;
        public ButtonEvent OnLeftTrackpadButton;
        public ButtonEvent OnRightTrackpadButton;
        public ButtonEvent OnLeftTrackpadTouch;
        public ButtonEvent OnRightTrackpadTouch;
        public AxisEvent OnLeftTrackpadTouchHAxis;
        public AxisEvent OnLeftTrackpadTouchVAxis;
        public AxisEvent OnRightTrackpadTouchHAxis;
        public AxisEvent OnRightTrackpadTouchVAxis;
        public ButtonEvent OnLeftTrigger;
        public AxisEvent OnLeftTriggerAxis;
        public ButtonEvent OnRightTrigger;
        public AxisEvent OnRightTriggerAxis;
        public AxisEvent OnLeftGripAxis;
        public AxisEvent OnRightGripAxis;

        public override void CheckInput()
        {
            if(Input.GetButton("ViveLeftTrigger"))
            {
                OnLeftTrigger.Invoke();
            }

            float LeftTriggerAxis = Input.GetAxis("ViveLeftTriggerAxis");
            if (LeftTriggerAxis > 0.5f)
            {
                OnLeftTriggerAxis.Invoke(LeftTriggerAxis);
            }

            if(Input.GetButton("ViveRightTrigger"))
            {
                OnRightTrigger.Invoke();
            }

            float RightTriggerAxis = Input.GetAxis("ViveRightTriggerAxis");
            if (RightTriggerAxis > 0.5f)
            {
                OnRightTriggerAxis.Invoke(RightTriggerAxis);
            }

        }

        public override string GetTypeAsString()
        {
            return "Vive";
        }
    }
}
