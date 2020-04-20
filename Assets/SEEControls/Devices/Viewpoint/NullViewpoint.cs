using UnityEngine;

namespace SEE.Controls.Devices
{
    public class NullViewpoint : Viewpoint
    {
        public override Vector2 Value
        {
            get => Vector3.zero;
        }

        public override bool Activated
        {
            get => false;
        }
    }
}
