using SEE.Utils;
using TinySpline;

namespace SEE.Layout
{
    /// <summary>
    /// Layout information about an edge.
    /// </summary>
    /// <typeparam name="T">Type of node this edge connects to.</typeparam>
    public interface ILayoutEdge<out T> where T : ILayoutNode
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
    }
}