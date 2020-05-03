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
        public abstract bool Activated { get; }

        /// <summary>
        /// True if the device is grabbing (e.g., when a particular grabbing key was pressed).
        /// </summary>
        public abstract bool IsGrabbing { get; }

        /// <summary>
        /// True if the device is releasing an object it has currently grabbed.
        /// </summary>
        public abstract bool IsReleasing { get; }
    }
}