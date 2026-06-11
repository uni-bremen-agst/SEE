using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// Stores the original start and end anchors of a line before line caps
    /// are applied and the visible line geometry is shortened.
    /// </summary>
    public class LineAnchorValueHolder : MonoBehaviour
    {
        /// <summary>
        /// Whether the original anchors have already been initialized with
        /// valid line endpoints.
        /// </summary>
        public bool HasOriginalAnchors;

        /// <summary>
        /// The original start anchor of the line before any line cap shortening.
        /// </summary>
        public Vector3 OriginalStartAnchor;

        /// <summary>
        /// The original end anchor of the line before any line cap shortening.
        /// </summary>
        public Vector3 OriginalEndAnchor;
    }
}
