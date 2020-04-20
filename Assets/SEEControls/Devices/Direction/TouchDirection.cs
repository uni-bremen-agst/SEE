using UnityEngine;
using InControl;

namespace SEE.Controls.Devices
{
    public class TouchDirection : Direction
    {
        public override Vector3 Value
        {
            get => GetDirection();
        }

        [Tooltip("The threshold determining whether a direction value has to be considered."), Range(0.001f, 1f)]
        public float Threshold = 0.3f;

        private Vector3 GetDirection()
        {
            // Use last device which provided input.
            InControl.InputDevice inputDevice = InputManager.ActiveDevice;

            // FIXME: Should this be moved to Update()?
            // Disable and hide touch controls if we use a controller.
            // If "Enable Controls On Touch" is ticked in Touch Manager inspector,
            // controls will be enabled and shown again when the screen is touched.
            //if (inputDevice != InControl.InputDevice.Null && inputDevice != TouchManager.Device)
            //{
            //    TouchManager.ControlsEnabled = false;
            //}

            Vector2 value = inputDevice.LeftStick.Value;
            return new Vector3(Discretize(value.x), 0, Discretize(value.y));
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
            