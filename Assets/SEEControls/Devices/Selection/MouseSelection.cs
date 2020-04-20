using UnityEngine;

namespace SEE.Controls.Devices
{
    public class MouseSelection: Selection
    {
        [Tooltip("The index of the mouse button needed to be pressed to change the viewpoint (0 = left, 1 = right).")]
        public int MouseButton = 0;

        public override Vector3 Value
        {
            get
            {
                return Input.mousePosition;
            }
        }

        public override bool Activated
        {
            get => Input.GetMouseButton(MouseButton);
        }
    }
}