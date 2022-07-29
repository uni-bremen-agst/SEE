using SEE.Layout.Utils;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Draws edges as straight lines at either above or below the game nodes.
    /// </summary>
    public class StraightEdgeLayout : IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        /// <param name="minLevelDistance">the minimal distance between different edge levels;
        /// here: the offset for the edge line w.r.t. its source and target block</param>
        public StraightEdgeLayout(bool edgesAboveBlocks, float minLevelDistance)
            : base(edgesAboveBlocks, minLevelDistance)
        {
            name = "Straight";
        }

        /// Adds way points to each edge in the given list of <paramref name="edges"/>
        /// along a leveled up line straight from its source to each target.
        /// The <paramref name="edges"/> are assumed to be in between pairs of nodes in
        /// the given set of <paramref name="nodes"/>. The given <paramref name="nodes"\>
        /// are used to determine the height at which to draw the edges so that they
        /// do not pass through any other node and, hence, should include every node that
        /// may be in between sources and targets of any edge in <paramref name="edges"/>.
        /// </summary>
        /// <param name="nodes">nodes whose edges are to be drawn (ignored)</param>
        /// <param name="edges">edges for which to add way points</param>
        public override void Create<T>(IEnumerable<T> nodes, IEnumerable<ILayoutEdge<T>> edges)
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
            // The level at which edges are drawn.
            float edgeLevel = edgesAboveBlocks ? maxBlockY + offset : minBlockY - offset;

            foreach (ILayoutEdge<T> edge in edges)
            {
                ILayoutNode source = edge.Source;
                ILayoutNode target = edge.Target;
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
                edge.Spline = CreateSpline(start, end, edgeLevel);
            }
        }

        /// <summary>
        /// Returns a spline (sequence of lines) from <paramref name="start"/> to <paramref name="end"/>
        /// on an offset straight line led on the given <paramref name="yLevel"/>. The first
        /// point is <paramref name="start"/>. The second point has the same x and z
        /// co-ordinate as <paramref name="start"/> but its y co-ordinate is <paramref name="yLevel"/>.
        /// The third point has the same x and z co-ordinate as <paramref name="end"/> but again
        /// its y co-ordinate is <paramref name="yLevel"/>. The last point is <paramref name="end"/>.
        /// </summary>
        /// <param name="start">start of the line</param>
        /// <param name="end">end of the line</param>
        /// <param name="yLevel">the y co-ordinate of the two other points in between <paramref name="start"/>
        /// and <paramref name="end"/></param>
        /// <returns>the four points of the offset straight line</returns>
        private TinySpline.BSpline CreateSpline(Vector3 start, Vector3 end, float yLevel)
        {
            Vector3[] points = new Vector3[4];
            points[0] = start;
            points[1] = points[0]; // we are maintaining the x and z co-ordinates,
            points[1].y = yLevel;   // but adjust the y co-ordinate
            points[2] = end;
            points[2].y = yLevel;
            points[3] = end;
            return new TinySpline.BSpline(4, 3, 1)
            {
                ControlPoints = TinySplineInterop.VectorsToList(points)
            };
        }
    }
}
