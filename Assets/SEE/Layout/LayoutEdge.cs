using SEE.Utils;
using TinySpline;

namespace SEE.Layout
{

    public abstract class LayoutEdge : LayoutEdge<ILayoutNode>
    {
        protected LayoutEdge(ILayoutNode source, ILayoutNode target) : base(source, target)
        {
            // Base constructor does everything we need.
        }
    }

    /// <summary>
    /// Layout information about an edge.
    /// </summary>
    /// <typeparam name="T">Type of node this edge connects to.</typeparam>
    public abstract class LayoutEdge<T> : ILayoutEdge<T> where T : ILayoutNode
    {
        /// <summary>
        /// Source of the edge.
        /// </summary>
        public T Source { get; }

        /// <summary>
        /// Target of the edge.
        /// </summary>
        public T Target { get; }

        /// <summary>
        /// The shaping spline of the edge. The default spline is a line
        /// (i.e., a spline of degree 1 with 2 control points) connecting the
        /// center position (<see cref="IGameNode.CenterPosition"/>) of
        /// <see cref="Source"/> and <see cref="Target"/>.
        /// </summary>
        public BSpline Spline { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source of the edge.</param>
        /// <param name="target">Target of the edge.</param>
        public LayoutEdge(T source, T target)
        {
            Source = source;
            Target = target;
            Spline = new BSpline(2, 3, 1)
            {
                ControlPoints = TinySplineInterop.VectorsToList(
                    Source.CenterPosition,
                    Target.CenterPosition)
            };
        }
    }
}
