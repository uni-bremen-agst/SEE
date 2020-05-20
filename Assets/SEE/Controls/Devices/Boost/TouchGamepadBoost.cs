using InControl;
using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing a boost factor for movements based on a gamepad.
    /// </summary>
    public class TouchGamepadBoost : Boost
    {
        [Tooltip("The value to be added to the boost factor for each increment."), Range(0.01f, 10.0f)]
        public float Delta = 0.1f;

        private void Update()
        {
            // For the standardized layout of gamepads supported by InControl, see:
            // http://www.gallantgames.com/pages/incontrol-standardized-controls

            if (InputManager.ActiveDevice.Action4.WasPressed) // Button X
            {
                boost += Delta;
            }
            else if (InputManager.ActiveDevice.Action3.WasPressed) // Button Y
            {
                boost -= Delta;
            }
            if (boost <= 0)
            {
                boost = 0.01f;
            }
        }
    }
}
