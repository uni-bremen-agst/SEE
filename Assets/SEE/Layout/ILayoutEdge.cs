using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Layout information about an edge.
    /// </summary>
    public class ILayoutEdge
    {
        /// <summary>
        /// Source of the edge.
        /// </summary>
        public ILayoutNode Source;
        /// <summary>
        /// Target of the edge.
        /// </summary>
        public ILayoutNode Target;
        /// <summary>
        /// The points of the polygone for rendering the edge.
        /// </summary>
        public Vector3[] Points;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source of the edge.</param>
        /// <param name="target">Target of the edge.</param>
        /// <param name="points">The points of the polygone for rendering the edge.</param>
        public ILayoutEdge(ILayoutNode source, ILayoutNode target, Vector3[] points)
        {
            this.Source = source;
            this.Target = target;
            this.Points = points;
        }
    }
}