using UnityEngine;
using InControl;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Input device providing direction through 2D co-ordinates on the screen.
    /// The input is retrieved from gamepad controllers or touch screens managed by InControl.
    /// </summary>
    public class TouchGamepadDirection : Direction
    {
        [Tooltip("The threshold determining whether a direction value has to be considered."), Range(0.001f, 1f)]
        public float Threshold = 0.3f;

        /// <summary>
        /// Selection direction by way of the left stick of the controller.
        /// Only the x and z co-oridnates of the result are relevant. The
        /// y co-ordinate is always 0.
        /// </summary>
        /// <returns>direction by way of the left stick of the controller</returns>
        public override Vector3 Value
        {
            get => GetDirection();
        }

        private Vector3 GetDirection()
        {
            // Use last device which provided input.
            InControl.InputDevice inputDevice = InputManager.ActiveDevice;
            Vector2 value = inputDevice.LeftStick.Value;
            return new Vector3(Discretize(value.x), 0, Discretize(value.y));
        }

        /// <summary>
        /// Disables and hides touch controls if we use a controller.
        /// If "Enable Controls On Touch" is ticked in InControl's Touch Manager inspector,
        /// controls will be enabled and shown again when the screen is touched.
        /// </summary>
        private void Update()
        {
            InControl.InputDevice inputDevice = InputManager.ActiveDevice;
            if (inputDevice != InControl.InputDevice.Null && inputDevice != TouchManager.Device)
            {
                TouchManager.ControlsEnabled = false;
            }
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

        // For debugging to obtain what kinds of controls an input device offers and
        // what their values are.
        //private void ReportDevice(InControl.InputDevice device, float threshold = 0.0f)
        //{
        //    foreach (var t in Enum.GetValues(typeof(InputControlType)).Cast<InputControlType>())
        //    {
        //        if (device.HasControl(t))
        //        {
        //            float value = device.GetControl(t).Value;
        //            bool wasPressed = device.GetControl(t).WasPressed;
        //            if (value >= threshold || wasPressed)
        //            {
        //                Debug.LogFormat("{0} {1} {2}\n", t, wasPressed, value);
        //            }
        //        }
        //    }
        //}

        // For debugging. Shows information on the current touches on the screen.
        //void OnGUI()
        //{
        //    var y = 10.0f;

        //    var touchCount = TouchManager.TouchCount;
        //    for (var i = 0; i < touchCount; i++)
        //    {
        //        var touch = TouchManager.GetTouch(i);
        //        var text = "" + i + ": fingerId = " + touch.fingerId;
        //        text = text + ", phase = " + touch.phase;
        //        text = text + ", startPosition = " + touch.startPosition;
        //        text = text + ", position = " + touch.position;

        //        if (touch.IsMouse)
        //        {
        //            text = text + ", mouseButton = " + touch.mouseButton;
        //        }

        //        GUI.Label(new Rect(10, y, Screen.width, y + 15.0f), text);
        //        y += 20.0f;
        //    }
        //}
    }
}
            