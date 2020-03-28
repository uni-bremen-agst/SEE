using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class SublayoutNode : AbstractSublayoutNode<Node>
    {

        public SublayoutNode(Node node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayouts) : base(node, innerNodeKinds, nodeLayouts)
        {

        }
    }
}
