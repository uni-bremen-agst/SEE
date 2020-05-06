using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A throttle input device based on the keyboard.
    /// </summary>
    public class KeyboardThrottle : Throttle
    {
        private const KeyCode throttleKey = KeyCode.LeftShift;

        /// <summary>
        /// The value is 1 if the left-shift key was pressed or 0 otherwise.
        /// </summary>
        public override float Value
        {
            get => Input.GetKey(throttleKey) ? 1 : 0;
        }
    }
}