using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class SublayoutNode
    {
        private string name;

        private List<ILayoutNode> nodes = new List<ILayoutNode>();

        private List<ILayoutNode> removedChildren = new List<ILayoutNode>();

        private InnerNodeKinds innerNodeKind;

        private NodeLayouts nodeLayout; 

        private ILayoutNode node;

        public SublayoutNode (string name, ILayoutNode node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayout)
        {
            this.name = name;
            this.Node = node;
            this.InnerNodeKind = innerNodeKinds;
            this.NodeLayout = nodeLayout;
        }

        public ILayoutNode Node { get => node; set => node = value; }
        public List<ILayoutNode> Nodes { get => nodes; set => nodes = value; }
        public InnerNodeKinds InnerNodeKind { get => innerNodeKind; set => innerNodeKind = value; }
        public NodeLayouts NodeLayout { get => nodeLayout; set => nodeLayout = value; }
        public List<ILayoutNode> RemovedChildren { get => removedChildren; set => removedChildren = value; }
    }
}

