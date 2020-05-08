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
        /// True if the device is activated (e.g., when a mouse button was pressed).
        /// </summary>
        public abstract bool IsSelecting { get; }

        /// <summary>
        /// True if the device is grabbing is toggled (e.g., when a particular grabbing 
        /// key was pressed).
        /// </summary>
        public abstract bool IsGrabbing { get; }

        /// <summary>
        /// The degree of pulling. A value between 0 and 1.
        /// </summary>
        public abstract float Pull { get; }
    }
}