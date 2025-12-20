using SEE.Utils;
using UnityEngine;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Abstract superclass of curvy (i.e., spline based) edge layouts.
    /// </summary>
    public abstract class CurvyEdgeLayoutBase : IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">If true, edges are drawn above nodes, otherwise below.</param>
        /// <param name="minLevelDistance">The minimal distance between different edge levels.</param>
        protected CurvyEdgeLayoutBase(bool edgesAboveBlocks, float minLevelDistance)
            : base(edgesAboveBlocks, minLevelDistance)
        {
        }

        /// <summary>
        /// Yields a spline for a self loop at a node. The first point
        /// is the front left corner of the roof/ground of <paramref name="node"/>
        /// and the last point is its opposite back right roof/ground corner. Thus, the
        /// edge is diagonal across the roof/ground. The peak of the spline is in the
        /// middle of the start and end, where the y co-ordinate of that peak
        /// is <paramref name="heightOffset"/> above the roof or below the ground,
        /// respectively.
        /// </summary>
        /// <param name="node">Node whose self loop line points are required.</param>
        /// <param name="edgesAboveBlocks">If true, the edges are drawn above the game nodes;
        /// otherwise below.</param>
        /// <param name="heightOffset">The offset of the middle point's y co-ordinate.</param>
        /// <returns>Line points forming a self loop above/below <paramref name="node"/>.</returns>
        protected static TinySpline.BSpline SelfLoop(ILayoutNode node, bool edgesAboveBlocks, float heightOffset)
        {
            // center area (roof or ground)
            Vector3 center = edgesAboveBlocks ? node.Roof : node.Ground;
            Vector3 extent = node.AbsoluteScale / 2.0f;
            // left front corner of center area
            Vector3 start = new(center.x - extent.x, center.y, center.z - extent.z);
            // right back corner of center area
            Vector3 end = new(center.x + extent.x, center.y, center.z + extent.z);
            Vector3 middle = center;
            middle.y += edgesAboveBlocks ? heightOffset : -heightOffset;
            return TinySpline.BSpline.InterpolateCubicNatural(
                TinySplineInterop.VectorsToList(start, middle, end), 3);
        }
    }
}