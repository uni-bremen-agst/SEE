using UnityEngine;

namespace SEE.Controls.Devices
{
    public class NullSelection : Selection
    {
        public override Vector3 Direction => Vector3.zero;

        public override bool Activated => false;

        public override Vector3 Position => Vector3.zero;
    }
}