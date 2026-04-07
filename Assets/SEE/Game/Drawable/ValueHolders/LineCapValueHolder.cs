using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// Stores the line cap for a line GameObject.
    /// This component is attached to a line and is used to persist
    /// start and end cap selection independently from the rendered geometry.
    /// </summary>
    public class LineCapValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The configuration of the line start cap.
        /// </summary>
        public LineCap StartCap { get; set; }

        /// <summary>
        /// The configuration of the line end cap.
        /// </summary>
        public LineCap EndCap { get; set; }
    }
}