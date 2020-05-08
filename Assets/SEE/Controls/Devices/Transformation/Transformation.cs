using UnityEngine;

namespace SEE.Controls.Devices
{
    public abstract class Transformation : InputDevice
    {
        /// <summary>
        /// The different kinds of transformation requests detected.
        /// </summary>
        public enum Kind
        {
            None,
            MoveLeft,
            MoveRight,
            MoveForward,
            MoveBackward,
            Zoom,
        }

        /// <summary>
        /// Yields the kind of transformation request detected or Kind.None
        /// if none has been recognized.
        /// </summary>
        /// <returns>transformation request detected</returns>
        public abstract Kind Recognize();

        /// <summary>
        /// The zoom factor. A value not smaller than 0. Defined only when 
        /// Recognize() = Kind.Zoom.
        /// </summary>
        public abstract float ZoomFactor { get; }
    }
}