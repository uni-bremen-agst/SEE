using SEE.Game;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class SublayoutLayoutNode : AbstractSublayoutNode<ILayoutNode>
    {

        public SublayoutLayoutNode(ILayoutNode node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayouts) : base(node, innerNodeKinds, nodeLayouts)
        {
            node.IsSublayoutRoot = true;
        }
    }
}

