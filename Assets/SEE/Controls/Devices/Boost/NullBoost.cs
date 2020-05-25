using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing a constant boost factor for movements that
    /// can be set in the Inspector (but not add runtime).
    /// </summary>
    public class NullBoost : Boost
    {
        public override float Value
        {
            get => boost;
        }
    }
}
