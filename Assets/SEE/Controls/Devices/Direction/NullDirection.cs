using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device for constant direction Vector3.zero. It can be used in
    /// settings where directions do not matter. 
    /// </summary>
    public class NullDirection : Direction
    {
        public override Vector3 Value
        {
            get => Vector3.zero;
        }
    }
}