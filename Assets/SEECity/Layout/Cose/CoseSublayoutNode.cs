using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.GraphSettings;

namespace SEE.Layout
{
    public class SublayoutNode
    {
        private string name;

        private List<Node> nodes = new List<Node>();

        private List<Node> removedChildren = new List<Node>();

        private InnerNodeKinds innerNodeKind;

        private NodeLayouts nodeLayout; 

        private Node node;

        public SublayoutNode (string name, Node node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayout)
        {
            this.name = name;
            this.Node = node;
            this.InnerNodeKind = innerNodeKinds;
            this.NodeLayout = nodeLayout;
        }

        public Node Node { get => node; set => node = value; }
        public List<Node> Nodes { get => nodes; set => nodes = value; }
        public InnerNodeKinds InnerNodeKind { get => innerNodeKind; set => innerNodeKind = value; }
        public NodeLayouts NodeLayout { get => nodeLayout; set => nodeLayout = value; }
        public List<Node> RemovedChildren { get => removedChildren; set => removedChildren = value; }
    }
}

