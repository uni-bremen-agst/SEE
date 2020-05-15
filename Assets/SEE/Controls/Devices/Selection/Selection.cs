using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class for all input devices providing selection information.
    /// </summary>
    public abstract class Selection : InputDevice
    {
        /// <summary>
        /// The direction of the selection. Subclasses may provide values where
        /// not all co-ordinates of the resulting vector are relevant (e.g., for
        /// mouse directions).
        /// </summary>
        public abstract Vector3 Direction { get; }

        /// <summary>
        /// The position of the selection device at the time of selection. Subclasses 
        /// may provide values where not all co-ordinates of the resulting vector are 
        /// relevant (e.g., for mouse positions).
        /// </summary>
        public abstract Vector3 Position { get; }

        /// <summary>
        /// The current action mode.
        /// </summary>
        public enum State
        {
            Idle,         // nothing requested by the user
            IsSelecting,  // the user wants to select an object
            IsGrabbing    // the user wants to grab an object
        }

        /// <summary>
        /// The current action.
        /// </summary>
        protected State state = State.Idle;

        /// <summary>
        /// The current action.
        /// </summary>
        public virtual State CurrentState { get => state; }

        /// <summary>
        /// The degree of pulling. A value between 0 and 1.
        /// </summary>
        public abstract float Pull { get; }
    }
}