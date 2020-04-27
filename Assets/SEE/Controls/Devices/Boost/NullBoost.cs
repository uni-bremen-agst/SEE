using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing a constant boost factor for movements that
    /// can be set in the Inspector (but not add runtime).
    /// </summary>
    public class NullBoost : Boost
    {
        [Tooltip("Boost factor for movements."), Range(0.01f, 10.0f)]
        public float boost = 1.0f;

        public override float Value
        {
            get => boost;
        }
    }
}
