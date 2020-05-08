using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device for selections yielding constant values. It can be
    /// used in situations where a selection device is not actually required.
    /// </summary>
    public class NullSelection : Selection
    {
        public override Vector3 Direction => Vector3.zero;

        public override bool IsSelecting => false;

        public override Vector3 Position => Vector3.zero;

        public override bool IsGrabbing => false;

        public override float Pull => 0;
    }
}