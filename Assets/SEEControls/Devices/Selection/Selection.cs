using UnityEngine;

namespace SEE.Controls.Devices
{
    public abstract class Selection : InputDevice
    {
        public abstract Vector3 Value { get; }

        public abstract bool Activated { get; }
    }
}