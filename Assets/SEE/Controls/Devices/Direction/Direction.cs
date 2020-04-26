using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class of all input devices providing a direction for
    /// movements.
    /// </summary>
    public abstract class Direction : InputDevice
    {
        public abstract Vector3 Value { get; }
    }
}