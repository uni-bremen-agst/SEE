using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    /// <summary>
    /// A class for holding properties for a sublayout 
    /// </summary>
    public class SublayoutLayoutNode : AbstractSublayoutNode<ILayoutNode>
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="node">the root node</param>
        /// <param name="innerNodeKinds">to inner node kind</param>
        /// <param name="nodeLayouts">the nodelayout of this sublayout</param>
        public SublayoutLayoutNode(ILayoutNode node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayouts) : base(node, innerNodeKinds, nodeLayouts)
        {
            node.IsSublayoutRoot = true;
        }
    }
}

