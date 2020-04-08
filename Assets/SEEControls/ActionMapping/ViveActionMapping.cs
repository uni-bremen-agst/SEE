using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    [CreateAssetMenu(fileName = "ViveActionMapping", menuName = "Controls/ViveActionMapping", order = 2)]
    public class ViveActionMapping : ActionMapping
    {
        public UnityEvent<bool> OnLeftMenuButton;
        public UnityEvent<bool> OnRightMenuButton;
        public UnityEvent<bool> OnLeftTrackpadButton;
        public UnityEvent<bool> OnRightTrackpadButton;
        public UnityEvent<bool> OnLeftTrackpadTouch;
        public UnityEvent<bool> OnRightTrackpadTouch;
        public UnityEvent<float> OnLeftTrackpadTouchHAxis;
        public UnityEvent<float> OnLeftTrackpadTouchVAxis;
        public UnityEvent<float> OnRightTrackpadTouchHAxis;
        public UnityEvent<float> OnRightTrackpadTouchVAxis;
        public UnityEvent<bool> OnLeftTrigger;
        public UnityEvent<float> OnLeftTriggerAxis;
        public UnityEvent<bool> OnRightTrigger;
        public UnityEvent<float> OnRightTriggerAxis;
        public UnityEvent<float> OnLeftGripAxis;
        public UnityEvent<float> OnRightGripAxis;

        public override void CheckInput()
        {
            if(Input.GetButton("ViveLeftTrigger"))
            {
                OnLeftTrigger.Invoke(true);
            }

            float LeftTriggerAxis = Input.GetAxis("ViveLeftTriggerAxis");
            if (LeftTriggerAxis > 0.5f)
            {
                OnLeftTriggerAxis.Invoke(LeftTriggerAxis);
            }

            if(Input.GetButton("ViveRightTrigger"))
            {
                OnRightTrigger.Invoke(true);
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
