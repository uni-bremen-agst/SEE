using UnityEngine;

namespace SEE.Controls.Devices
{
    public abstract class Selection : InputDevice
    {
        public abstract Vector3 Direction { get; }

        public abstract Vector3 Position { get; }

        public abstract bool Activated { get; }
    }
}