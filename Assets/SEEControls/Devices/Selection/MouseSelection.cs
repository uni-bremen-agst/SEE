using UnityEngine;

namespace SEE.Controls.Devices
{
    public class MouseSelection: Selection
    {
        [Tooltip("The index of the mouse button needed to be pressed to change the viewpoint (0 = left, 1 = right).")]
        public int MouseButton = 0;

        public override Vector3 Direction => Input.mousePosition;

        public override bool Activated => Input.GetMouseButton(MouseButton);        

        public override Vector3 Position => Input.mousePosition;
    }
}