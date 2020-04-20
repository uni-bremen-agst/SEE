namespace SEE.Controls.Devices
{
    public class MouseBoost : Boost
    {
        public override float Value
        {
            get =>
            // Input.mouseScrollDelta is stored in a Vector2.y property. (The Vector2.x 
            // value is ignored.) Input.mouseScrollDelta can be positive (up) or negative (down). 
            // The value is zero when the mouse scroll is not rotated. Note that a mouse with 
            // a center scroll wheel is typical on a PC. Modern macOS uses double finger movement 
            // up and down on the trackpad to emulate center scrolling. The value returned by 
            // mouseScrollDelta will need to be adjusted according to the scroll rate.
            UnityEngine.Input.mouseScrollDelta.y;
        }
    }
}
