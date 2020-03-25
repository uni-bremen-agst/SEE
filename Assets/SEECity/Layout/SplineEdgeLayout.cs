using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Draws edges as splines with three control points between either the roof or ground of
    /// game objects.
    /// </summary>
    public class SplineEdgeLayout : IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        public SplineEdgeLayout(bool edgesAboveBlocks) : base(edgesAboveBlocks)
        {
            name = "Splines";
        }

        public override ICollection<LayoutEdge> Create(ICollection<ILayoutNode> layoutNodes)
        {
            ICollection<LayoutEdge> layout = new List<LayoutEdge>();
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
                    layout.Add(new LayoutEdge(source, target, LinePoints.SplineLinePoints(start, end, edgesAboveBlocks)));
                }
            }
            return layout;
        }
    }
}
