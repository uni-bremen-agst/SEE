using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class of all input devices providing a boost factor for movements.
    /// </summary>
    public abstract class Boost : InputDevice
    {
        [Tooltip("Factor by which speed is multiplied. A value of 1 is neutral."), Range(0.01f, 10.0f)]
        public float boost = 1.0f;

        public virtual float Value
        {
            get => boost;
        }
    }
}
