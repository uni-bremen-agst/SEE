using SEE.DataModel.DG;
using SEE.Layout;

namespace SEE.Game
{
    /// <summary>
    /// Implementation of ILayoutEdge that also stores the underlying graph edge that is 
    /// visualized by this layout edge.
    /// </summary>
    public class LayoutEdge : ILayoutEdge
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">source node of the layout edge</param>
        /// <param name="target">target node of the layout edge</param>
        /// <param name="edge">the underlying graph edge that is visualized by this layout edge</param>
        public LayoutEdge(ILayoutNode source, ILayoutNode target, Edge edge)
            : base(source, target)
        {
            ItsEdge = edge;
        }

        /// <summary>
        /// The underlying graph edge that is visualized by this layout edge.
        /// </summary>
        public Edge ItsEdge
        {
            get; private set;
        }
    }
}