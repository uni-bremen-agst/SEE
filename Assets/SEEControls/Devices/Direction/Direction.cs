using UnityEngine;

namespace SEE.Controls.Devices
{
    public abstract class Direction : InputDevice
    {
        public abstract Vector3 Value { get; }
    }
}