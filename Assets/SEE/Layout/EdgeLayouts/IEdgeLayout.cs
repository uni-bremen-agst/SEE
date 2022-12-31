using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Shared abstract super class of all edge layouts.
    /// </summary>
    public abstract class IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        /// <param name="minLevelDistance">the minimal distance between different edge levels</param>
        public IEdgeLayout(bool edgesAboveBlocks, float minLevelDistance)
        {
            this.edgesAboveBlocks = edgesAboveBlocks;
            this.minLevelDistance = minLevelDistance;
        }

        /// <summary>
        /// The unique name of a layout.
        /// </summary>
        public string Name
        {
            get => name;
        }

        /// <summary>
        /// Adds way points to the given <paramref name="edges"/> according to the layout.
        /// The <paramref name="edges"/> are assumed to be in between pairs of nodes in
        /// the given set of <paramref name="nodes"/>. For hierarchical edge layouts,
        /// <paramref name="nodes"/> must include all ancestors for all nodes that are
        /// source or target of any edge in the given set of <paramref name="edges"/>.
        /// </summary>
        /// <param name="nodes">nodes whose edges are to be drawn or which are
        /// ancestors of any nodes whose edges are to be drawn</param>
        /// <param name="edges">edges for which to add way points</param>
        public abstract void Create<T>(IEnumerable<T> nodes, IEnumerable<ILayoutEdge<T>> edges)
            where T : ILayoutNode, IHierarchyNode<ILayoutNode>;

        /// <summary>
        /// Name of the layout.
        /// </summary>
        protected string name = "";

        /// <summary>
        /// Orientation of the edges;
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses;
        /// </summary>
        protected readonly bool edgesAboveBlocks;

        /// <summary>
        /// The minimal distance between different edge levels.
        /// </summary>
        protected readonly float minLevelDistance;

        /// <summary>
        /// Yields the greatest and smallest y co-ordinate and the maximal height (all in
        /// world space) of all given <paramref name="nodes"/>.
        ///
        /// Precondition: <paramref name="nodes"/> is not empty.
        /// </summary>
        /// <param name="nodes">list of nodes whose greatest and smallest y co-ordinate is required</param>
        /// <param name="minY">smallest y world co-ordinate</param>
        /// <param name="maxY">largest y world co-ordinate</param>
        /// <param name="maxHeight">maximal height of nodes in world scale</param>
        protected static void MinMaxBlockY<T>(IEnumerable<T> nodes, out float minY, out float maxY, out float maxHeight)
        where T : ILayoutNode
        {
            maxY = Mathf.NegativeInfinity;
            minY = Mathf.Infinity;
            maxHeight = 0.0f;
            foreach (T node in nodes)
            {
                float cy = node.CenterPosition.y;
                float height = node.AbsoluteScale.y;
                {
                    float roof = cy + height / 2.0f;
                    if (roof > maxY)
                    {
                        maxY = roof;
                    }
                }
                {
                    float ground = cy - height / 2.0f;
                    if (ground < minY)
                    {
                        minY = ground;
                    }
                }
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
        }

        /// <summary>
        /// Simplifies the given polyline. This function uses the Ramer–Douglas–Peucker
        /// (RDP) algorithm to identify and remove points whose distances fall below
        /// <paramref name="epsilon"/> (with respect to the line drawn between their
        /// neighbors). The greater <paramref name="epsilon"/> is, the more aggressively
        /// points are removed (note: values greater than one are fine). A positive value
        /// close to zero results in a line with little to no reduction. A negative value
        /// is treated as 0. A value of zero has no effect.
        ///
        /// Precondition: <paramref name="polyLine"/> is not null.
        /// Postcondition: The lenght of the returned array is less than or equal to the
        /// lenght of <paramref name="polyLine"/>.
        /// </summary>
        /// <param name="polyLine">The polyline to simplify.</param>
        /// <param name="epsilon">Used to evaluate which points should be removed from
        /// <paramref name="polyLine"/>. Values less than 0 are mapped to 0.</param>
        /// <returns>A similar polyline with the same amount or fewer points.</returns>
        protected static Vector3[] Simplify(Vector3[] polyLine, float epsilon)
        {
            epsilon = Mathf.Max(0, epsilon);
            // Unity already includes a suitable implemantation of the rdp algroithm.
            List<Vector3> list = new List<Vector3>(polyLine.Length);
            list.AddRange(polyLine);
            LineUtility.Simplify(list, epsilon, list);
            return list.ToArray();
        }
    }
}
