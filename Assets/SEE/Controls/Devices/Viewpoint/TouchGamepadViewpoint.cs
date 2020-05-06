using UnityEngine;
using InControl;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A viewpoint device based on a gamepad controller or touch screen based on InControl.
    /// </summary>
    public class TouchGamepadViewpoint : Viewpoint
    {
        [Tooltip("The threshold determining whether a viewpoint value has to be considered."), Range(0.001f, 1f)]
        public float Threshold = 0.3f;

        /// <summary>
        /// Yields the direction derived from the active InControl input device's
        /// right stick.
        /// </summary>
        public override Vector2 Value
        {
            get => GetDirection();
        }

        /// <summary>
        /// True if the active InControl input device's right stick was pressed.
        /// </summary>
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

        /// <summary>
        /// Rounds the given <paramref name="value"/> to an integer value as follows:
        ///  1 if the value is greater than the threshold
        /// -1 if the value is smaller than the threshold
        /// 0 otherwise
        /// </summary>
        /// <param name="value">value to be discretized</param>
        /// <returns>discrete value</returns>
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
