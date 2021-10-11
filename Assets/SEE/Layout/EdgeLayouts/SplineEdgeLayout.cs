using SEE.Layout.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Draws edges as splines with three control points between either the roof or ground of
    /// game objects.
    /// </summary>
    public class SplineEdgeLayout : IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// 
        /// Parameter <paramref name="rdp"/> specifies the extent the polylines of the generated
        /// splines are simplified. Neighboring line points whose distances fall below 
        /// <paramref name="rdp"/> (with respect to the line drawn between their neighbors) will 
        /// be removed. The greater the value is, the more aggressively points are removed 
        /// (note: values greater than one are fine). A positive value close to zero results 
        /// in a line with little to no reduction. A negative value is treated as 0. A value 
        /// of zero has no effect.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        /// <param name="minLevelDistance">the minimal distance between different edge levels</param>
        /// <param name="rdp">epsilon parameter of the Ramer–Douglas–Peucker algorithm</param>
        public SplineEdgeLayout(bool edgesAboveBlocks, float minLevelDistance, float rdp = 0.0f)
            : base(edgesAboveBlocks, minLevelDistance)
        {
            name = "Splines";
            this.rdp = rdp;
        }

        /// <summary>
        /// Determines to which extent the polylines of the generated splines are simplified.
        /// </summary>
        private readonly float rdp = 0.0f; // 0.0f means no simplification

        /// <summary>
        /// Adds way points to each edge in the given list of<paramref name= "edges" />
        /// along a spline from its source to each target.
        /// The <paramref name="edges"/> are assumed to be in between pairs of nodes in
        /// the given set of <paramref name="nodes"/>. The given <paramref name="nodes"\>
        /// are used to determine the height at which to draw the edges so that they
        /// do not pass through any other node and, hence, should include every node that
        /// may be in between sources and targets of any edge in <paramref name="edges"/>.
        /// </summary>
        /// <param name="nodes">nodes whose edges are to be drawn or which are 
        /// ancestors of any nodes whose edges are to be drawn</param>
        /// <param name="edges">edges for which to add way points</param>
        public override void Create(ICollection<ILayoutNode> nodes, ICollection<ILayoutEdge> edges)
        {
            MinMaxBlockY(nodes, out float minBlockY, out float maxBlockY, out float maxHeight);

            // The offset of the edges above or below the ground chosen relative 
            // to the height of the largest block.
            // We are using a value relative to the highest node so that edges 
            // are farther away from the blocks for cities with large blocks and
            // closer to blocks for cities with small blocks. This may help to 
            // better read the edges.
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = Mathf.Max(minLevelDistance, 0.2f * maxHeight); // must be positive

            foreach (ILayoutEdge edge in edges)
            {
                ILayoutNode source = edge.Source;
                ILayoutNode target = edge.Target;
                // define the points along the line
                Vector3 start;
                Vector3 end;
                if (edgesAboveBlocks)
                {
                    start = source.Roof;
                    end = target.Roof;
                }
                else
                {
                    start = source.Ground;
                    end = target.Ground;
                }
                edge.Spline = CreateSpline(start, end, edgesAboveBlocks, offset);
            }
        }

        /// <summary>
        /// Returns a direct spline from <paramref name="start"/> to <paramref name="end"/>.
        /// The y co-ordinate of the middle point of the spline is X above (if <paramref name="above"/>
        /// is true) or below (if <paramref name="above"/> is false), respectively, the higher
        /// (or lower if <paramref name="above"/> is false) of the two (<paramref name="start"/>
        /// and <paramref name="end"/>), where X is half the distance between <paramref name="start"/>
        /// and <paramref name="end"/>.
        ///
        /// The y offset of the middle point is chosen relative to the distance between the two points.
        /// We are using a value relative to the distance so that splines connecting close points do
        /// not shoot into the sky. Otherwise they would be difficult to read. Likewise, splines
        /// connecting two points farther away should go higher (or deeper, respectively) so that we
        /// avoid crossings with things that may be in between. This heuristic may help to better read
        /// the splines.
        /// </summary>
        /// <param name="start">starting point</param>
        /// <param name="end">ending point</param>
        /// <param name="above">whether middle point of the spline should be above <paramref name="start"/>
        /// <param name="minOffset">the minimal y offset for the point in between <paramref name="start"/>
        /// and <paramref name="end"/> through which the spline should pass</param>
        /// <returns>points of the spline</returns>
        private TinySpline.BSpline CreateSpline(Vector3 start, Vector3 end, bool above, float minOffset)
        {
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = Mathf.Max(minOffset, 0.5f * Vector3.Distance(start, end)); // must be positive
            // The level at which edges are drawn.
            float edgeLevel = above ? Mathf.Max(start.y, end.y) + offset
                                    : Mathf.Min(start.y, end.y) - offset;

            Vector3 middle = Vector3.Lerp(start, end, 0.5f);
            middle.y = edgeLevel;
            return TinySpline.BSpline.InterpolateCubicNatural(
                TinySplineInterop.VectorsToList(start, middle, end), 3);
        }
    }
}
