using UnityEngine;
using InControl;

namespace SEE.Controls.Devices
{
    public class TouchViewpoint : Viewpoint
    {
        [Tooltip("The threshold determining whether a viewpoint value has to be considered."), Range(0.001f, 1f)]
        public float Threshold = 0.3f;

        public override Vector2 Value
        {
            get => GetDirection();
        }

        public override bool Activated
        {
            get => InputManager.ActiveDevice.RightStick.IsPressed;
        }

        private Vector2 GetDirection()
        {
            // Use last device which provided input.
            InControl.InputDevice inputDevice = InputManager.ActiveDevice;

            Vector2 value = inputDevice.RightStick.Value;
            return new Vector2(Discretize(value.x), Discretize(value.y));
        }

        private float Discretize(float value)
        {
            if (value > Threshold)
            {
                return 1;
            }
            else if (value < -Threshold)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
