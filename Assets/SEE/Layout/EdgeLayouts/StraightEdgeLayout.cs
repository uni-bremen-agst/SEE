using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
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
        /// <param name="scaleFactor">factor by which certain aspects of an edge are scaled;
        /// here: the offset for the edge line w.r.t. its source and target block</param>
        public StraightEdgeLayout(bool edgesAboveBlocks, float scaleFactor) 
            : base(edgesAboveBlocks, scaleFactor)
        {
            name = "Straight";
        }

        public override ICollection<LayoutEdge> Create(ICollection<ILayoutNode> layoutNodes)
        {
            ICollection<LayoutEdge> layout = new List<LayoutEdge>();

            MinMaxBlockY(layoutNodes, out float minBlockY, out float maxBlockY, out float maxHeight);

            // The offset of the edges above or below the ground chosen relative 
            // to the height of the largest block.
            // We are using a value relative to the highest node so that edges 
            // are farther away from the blocks for cities with large blocks and
            // closer to blocks for cities with small blocks. This may help to 
            // better read the edges.
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = 0.2f * maxHeight * scaleFactor; // must be positive
            // The level at which edges are drawn.
            float edgeLevel = edgesAboveBlocks ? maxBlockY + offset : minBlockY - offset;

            foreach (ILayoutNode source in layoutNodes)
            {
                foreach (ILayoutNode target in source.Successors)
                {
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
                    layout.Add(new LayoutEdge(source, target, LinePoints.StraightLinePoints(start, end, edgeLevel)));
                }
            }
            return layout;
        }
    }
}
