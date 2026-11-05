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

        /// <summary>
        /// Whether the line currently uses an explicit open renderer geometry
        /// to represent a loop whose start and end are shortened independently
        /// for line caps.
        /// </summary>
        public bool UsesExplicitLoopCapGeometry;

        /// <summary>
        /// Whether this open line retains the end cap of a split loop
        /// and may use the preceding segment for its visual cap geometry.
        /// </summary>
        public bool UsePreviousSegmentForEndCap;

        /// <summary>
        /// The original renderer positions before an explicit loop-cap geometry
        /// was created.
        /// </summary>
        public Vector3[] OriginalRendererPositions;
    }
}
