using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Input from a mouse. Supports trigger, buttonB, pointingDirection, and scroll.
    /// </summary>
    public class MouseDevice : InputDevice
    {
        private const int MouseButtonA = 0;
        private const int MouseButtonB = 1;

        private void Update()
        {
            if (Input.GetMouseButton(MouseButtonA))
            {
                trigger.Invoke(1);
            }
            else
            {
                trigger.Invoke(0);
            }
            if (Input.GetMouseButtonDown(MouseButtonB))
            {
                buttonB.Invoke(true);
            }
            if (Input.GetMouseButtonUp(MouseButtonB))
            {
                buttonB.Invoke(false);
            }
            movementDirection.Invoke(GetDirection());
            pointingDirection.Invoke(Input.mousePosition);
            scroll.Invoke(Scroll());
        }

        private Vector3 GetDirection()
        {
            // Input.GetAxis(axis): The value will be in the range -1...1 for keyboard and 
            // joystick input. If the axis is setup to be delta mouse movement, the mouse 
            // delta is multiplied by the axis sensitivity and the range is not -1...1.
            return new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0);
        }

        /// <summary>
        /// Returns the current mouse scroll delta.
        /// </summary>
        /// <returns>current mouse scroll delta</returns>
        private float Scroll()
        { 
            // Input.mouseScrollDelta is stored in a Vector2.y property. (The Vector2.x 
            // value is ignored.) Input.mouseScrollDelta can be positive (up) or negative (down). 
            // The value is zero when the mouse scroll is not rotated. Note that a mouse with 
            // a center scroll wheel is typical on a PC. Modern macOS uses double finger movement 
            // up and down on the trackpad to emulate center scrolling. The value returned by 
            // mouseScrollDelta will need to be adjusted according to the scroll rate.
            return Input.mouseScrollDelta.y;
        }
    }
}