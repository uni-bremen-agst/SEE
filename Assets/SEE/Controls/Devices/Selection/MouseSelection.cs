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

        public override bool Activated => Input.GetMouseButton(SelectionMouseButton);        

        public override Vector3 Position => Input.mousePosition;

        public override bool IsGrabbing => DoubleClick();

        public override bool IsReleasing => DoubleClick(); 

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