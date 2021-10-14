using Assets.SEE.GameObjects;
using SEE.Layout.Utils;
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
        /// The shaping spline of the edge. The default spline is a line
        /// (i.e., a spline of degree 1 with 2 control points) connecting the
        /// points (0, 0, 0) and (1, 1, 1).
        /// </summary>
        public TinySpline.BSpline Spline { get; set; } =
            new TinySpline.BSpline(2, 3, 1)
            {
                ControlPoints = {0, 0, 0, 1, 1, 1}
            };

        public Vector3[] Points =>
            TinySplineInterop.ListToVectors(Spline.Sample(100));

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
