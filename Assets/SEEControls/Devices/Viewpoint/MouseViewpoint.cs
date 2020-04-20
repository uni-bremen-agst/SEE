using UnityEngine;

namespace SEE.Controls.Devices
{
    public class MouseViewpoint : Viewpoint
    {
        [Tooltip("The index of the mouse button needed to be pressed to change the viewpoint (0 = left, 1 = right).")]
        public int MouseButton = 1;

        public override Vector2 Value
        {
            get
            {
                // Input.GetAxis(axis): The value will be in the range -1...1 for keyboard and 
                // joystick input. If the axis is setup to be delta mouse movement, the mouse 
                // delta is multiplied by the axis sensitivity and the range is not -1...1.
                return new Vector2(Input.GetAxis(MouseXActionName), Input.GetAxis(MouseYActionName));
            }
        }

        public override bool Activated
        {
            get => Input.GetMouseButton(MouseButton);
        }
    }
}
