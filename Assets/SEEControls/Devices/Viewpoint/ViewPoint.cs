using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class of all input devices providing information for the viewpoint,
    /// that is, the rotation of the camera.
    /// </summary>
    public abstract class Viewpoint : InputDevice
    {
        /// <summary>
        /// The focus of the viewpoint in 2D. It is used to center the screen.
        /// </summary>
        public abstract Vector2 Value { get; }
        /// <summary>
        /// True if the viewpoint device was activated. For instance, to change the
        /// viewpoint, a mouse button may need to be activated.
        /// </summary>
        public abstract bool Activated { get; }
    }
}