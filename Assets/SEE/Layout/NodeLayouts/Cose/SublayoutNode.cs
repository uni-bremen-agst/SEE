using SEE.DataModel;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    /// <summary>
    /// A class for holding properties for a sublayout 
    /// </summary>
    public class SublayoutNode : AbstractSublayoutNode<Node>
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="node">the root node</param>
        /// <param name="innerNodeKinds">to inner node kind</param>
        /// <param name="nodeLayouts">the nodelayout of this sublayout</param>
        public SublayoutNode(Node node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayouts) : base(node, innerNodeKinds, nodeLayouts)
        {

        }
    }
}
