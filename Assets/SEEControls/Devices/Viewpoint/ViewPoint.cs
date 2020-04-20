using UnityEngine;

namespace SEE.Controls.Devices
{
    public abstract class Viewpoint : InputDevice
    {
        public abstract Vector2 Value { get; }
        public abstract bool Activated { get; }
    }
}