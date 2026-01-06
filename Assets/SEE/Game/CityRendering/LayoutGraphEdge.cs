using SEE.DataModel.DG;
using SEE.Layout;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implementation of ILayoutEdge that also stores the underlying graph edge that is
    /// visualized by this layout edge.
    /// </summary>
    /// <typeparam name="T">Type of node this edge connects to.</typeparam>
    public class LayoutGraphEdge<T> : LayoutEdge<T> where T : ILayoutNode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source node of the layout edge.</param>
        /// <param name="target">Target node of the layout edge.</param>
        /// <param name="edge">The underlying graph edge that is visualized by this layout edge.</param>
        public LayoutGraphEdge(T source, T target, Edge edge)
            : base(source, target)
        {
            ItsEdge = edge;
        }

        /// <summary>
        /// The underlying graph edge that is visualized by this layout edge.
        /// </summary>
        public Edge ItsEdge
        {
            get;
        }
    }
}