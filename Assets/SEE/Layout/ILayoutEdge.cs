using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Layout information about an edge.
    /// </summary>
    public abstract class ILayoutEdge
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
        /// The control points of the polygon for rendering the edge.
        /// </summary>
        public Vector3[] ControlPoints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source of the edge.</param>
        /// <param name="target">Target of the edge.</param>
        public ILayoutEdge(ILayoutNode source, ILayoutNode target)
        {
            Source = source;
            Target = target;
        }
    }
}
