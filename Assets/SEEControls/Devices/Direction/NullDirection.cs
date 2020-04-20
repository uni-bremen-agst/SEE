using UnityEngine;

namespace SEE.Controls.Devices
{
    public class NullDirection : Direction
    {
        public override Vector3 Value
        {
            get => Vector3.zero;
        }
    }
}