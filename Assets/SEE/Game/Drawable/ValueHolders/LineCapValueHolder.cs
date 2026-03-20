using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// Stores the line cap configuration for a line GameObject.
    /// This component is attached to a line and is used to persist
    /// start and end cap settings independently from the rendered geometry.
    /// </summary>
    public class LineCapValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The configuration of the line start cap.
        /// </summary>
        public LineCapConf StartCap;

        /// <summary>
        /// The configuration of the line end cap.
        /// </summary>
        public LineCapConf EndCap;
    }
}