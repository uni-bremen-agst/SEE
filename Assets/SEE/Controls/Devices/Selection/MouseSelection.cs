using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// A selection device using input from a mouse or touch screen yielding 2D co-ordinates 
    /// for selected points on the screen. A user can hit the selection mouse button
    /// or touch the screen with a finger or pen.
    /// </summary>
    public class MouseSelection: Selection
    {
        [Tooltip("The index of the mouse button needed to be pressed to change the viewpoint (0 = left, 1 = right).")]
        public int SelectionMouseButton = 0;

        public override Vector3 Direction => Input.mousePosition;

        public override Vector3 Position => Input.mousePosition;

        private float pullDegree = 0.0f;

        public const float MouseWheelScale = 0.1f;

        public override float Pull
        {
            get
            {
                // Input.mouseScrollDelta is stored in a Vector2.y property. (The Vector2.x value is ignored.) 
                // Input.mouseScrollDelta can be positive (up) or negative (down). The value is zero when the 
                // mouse scroll is not rotated. Note that a mouse with a center scroll wheel is typical on 
                // a PC. Modern macOS uses double finger movement up and down on the trackpad to emulate 
                // center scrolling. The value returned by mouseScrollDelta will need to be adjusted according 
                // to the scroll rate. 
                float delta = Input.mouseScrollDelta.y * MouseWheelScale;
                // When the wheel is turned towards the hand, delta is negative; otherwise positive.
                // Intuitively, one would interpret wheel turns towards the hand as trying to draw 
                // an object towards the hand and vice versa. Hence, we need to negate delta.
                pullDegree = Mathf.Clamp(-delta, -1, 1);
                return pullDegree;
            }
        }

        private void Update()
        {
            State nextState = State.Idle;
            if (Input.GetMouseButtonDown(SelectionMouseButton))
            {
                if (!oneClick)
                {
                    // This is the first click.
                    oneClick = true;
                    timeOfLastClick = Time.time;
                    nextState = State.IsSelecting;
                }
                else
                {
                    // found a double click => reset
                    oneClick = false;
                    // has the double click happened in the waiting period?
                    if ((Time.time - timeOfLastClick) <= maxDelay)
                    {
                        nextState = State.IsGrabbing;
                    }
                }
            }
            if (oneClick)
            {
                // if the time now is maxDelay seconds after the first click, we need to reset
                if ((Time.time - timeOfLastClick) > maxDelay)
                {
                    oneClick = false;
                }
            }
            state = nextState;
        }

        /// <summary>
        /// True if the first click of an expected double click happened.
        /// </summary>
        private bool oneClick = false;
        /// <summary>
        /// The point in time since game start of the last single click.
        /// </summary>
        private float timeOfLastClick;
        /// <summary>
        /// The maximal time in seconds we allow between two clicks to
        /// consider them a double click.
        /// </summary>
        private readonly float maxDelay = 0.5f;
    }
}