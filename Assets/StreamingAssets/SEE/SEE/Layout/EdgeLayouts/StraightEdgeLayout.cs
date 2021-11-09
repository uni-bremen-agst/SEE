using SEE.Layout.Utils;
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
            // The level at which edges are drawn.
            float edgeLevel = edgesAboveBlocks ? maxBlockY + offset : minBlockY - offset;

            Debug.LogFormat("offset={0} edgeLevel={1} maxHeight={2}\n", offset, edgeLevel, maxHeight);
            foreach (ILayoutEdge edge in edges)
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
                edge.Points = LinePoints.StraightLinePoints(start, end, edgeLevel);
            }
        }
    }
}
