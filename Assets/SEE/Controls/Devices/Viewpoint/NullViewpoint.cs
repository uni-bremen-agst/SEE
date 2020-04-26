using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device for viewpoint information yielding constant values.
    /// It can be used in situations where a viewpoint is not actually needed.
    /// For instance, in virtual reality where the user wears a head-mounted
    /// display, the viewpoint is adjusted automatically by SteamVR.
    /// </summary>
    public class NullViewpoint : Viewpoint
    {
        /// <summary>
        /// Always Vector3.zero.
        /// </summary>
        public override Vector2 Value
        {
            get => Vector3.zero;
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public override bool Activated
        {
            get => false;
        }
    }
}
