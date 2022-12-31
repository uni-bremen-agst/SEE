using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Custom edge layout for the architecture graph.
    /// The layout creates plane two point lines between the center points of two nodes.
    /// </summary>
    public class ArchitectureEdgeLayout : IEdgeLayout
    {
        public ArchitectureEdgeLayout(bool edgesAboveBlocks, float minLevelDistance) : base(edgesAboveBlocks, minLevelDistance)
        {
            name = "Flat straight edge";
        }

        public override void Create<T>(IEnumerable<T> nodes, IEnumerable<ILayoutEdge<T>> edges)
        {
            foreach (ILayoutEdge<T> edge in edges)
            {
                ILayoutNode source = edge.Source;
                ILayoutNode target = edge.Target;
                Vector3 start = source.CenterPosition;
                Vector3 end = target.CenterPosition;
                edge.Spline = CreateSpline(start, end);
            }
        }

        /// <summary>
        /// Returns a direct line (straight spline) from <paramref name="start"/> to <paramref name="end"/>.
        /// </summary>
        /// <param name="start">start of the line</param>
        /// <param name="end">end of the line</param>
        /// <returns>the two points of the straight line</returns>
        private TinySpline.BSpline CreateSpline(Vector3 start, Vector3 end)
        {
            Vector3[] points = new Vector3[2];
            points[0] = start;
            points[1] = end;
            return new TinySpline.BSpline(2, 3, 1)
            {
                ControlPoints = SEE.Utils.TinySplineInterop.VectorsToList(points)
            };
        }
    }
}