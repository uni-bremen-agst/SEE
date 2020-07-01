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

        [Tooltip("The least amount of seconds the mouse button must have been pressed to be considered grabbing."),
         Range(0.01f, 1.0f)]
        public float ButtonDurationThreshold = 0.5f;

        public override Vector3 Direction => Input.mousePosition;

        public override Vector3 Position => Input.mousePosition;

        private enum State
        {
            Idle,
            Selecting,
            Grabbing
        }

        private State state = State.Idle;

        /// <summary>
        /// Time since the start of a selection in seconds.
        /// </summary>
        private float timeButtonHeld = 0.0f;

        private void Update()
        {
            if (Input.GetMouseButtonDown(SelectionMouseButton))
            {
                state = State.Selecting;
                timeButtonHeld = Time.realtimeSinceStartup;
            }
            if (state == State.Selecting
                && Input.GetMouseButton(SelectionMouseButton) 
                && Time.realtimeSinceStartup - timeButtonHeld >= ButtonDurationThreshold)
            {
                state = State.Grabbing;
            }
            if (Input.GetMouseButtonUp(SelectionMouseButton) || IsCanceling)
            {
                state = State.Idle;
                timeButtonHeld = float.PositiveInfinity;
            }
        }

        public override bool IsSelecting => state == State.Selecting;

        public override bool IsGrabbing => state == State.Grabbing;

        /// <summary>
        /// The degree by which the mouse wheel was turned. A value in the range
        /// [-1, 1].
        /// </summary>
        private float pullDegree = 0.0f;

        /// <summary>
        /// The delta to be added to the pullDegree for each mouse wheel increment.
        /// </summary>
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

        public override bool IsCanceling => Input.GetKeyDown(KeyCode.C);

        public override bool IsZoomingIn => Input.GetKeyDown(KeyCode.I);

        public override bool IsZoomingOut => Input.GetKeyDown(KeyCode.O);

        public override bool IsZoomingHome => Input.GetKeyDown(KeyCode.R);

        /// <summary>
        /// Resets the selection timer, so the start of grabbing is delayed.
        /// </summary>
        public void ResetSelectionTimer()
        {
            if (state == State.Selecting)
            {
                timeButtonHeld = Time.realtimeSinceStartup;
            }
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

        /// <summary>
        /// True if the user presses the SelectionMouseButton twice with a maximal delay
        /// of mayDelay.
        /// </summary>
        /// <returns>true if user double clicks</returns>
        private bool DoubleClick()
        {
            if (Input.GetMouseButtonDown(SelectionMouseButton))
            {
                if (!oneClick) 
                {
                    // This is the first click.
                    oneClick = true;
                    timeOfLastClick = Time.time;
                    return false;
                }
                else
                {
                    // found a double click => reset
                    oneClick = false;
                    // has the double click happened in the waiting period?
                    return (Time.time - timeOfLastClick) <= maxDelay;
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
            return false;
        }
    }
}