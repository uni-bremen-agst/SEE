using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public abstract class AbstractSublayoutNode<T>
    {
        public T Node { get; }

        public List<T> RemovedChildren { get; set; }

        public List<T> Nodes { get; set; }

        public InnerNodeKinds InnerNodeKind { get; }

        public NodeLayouts NodeLayout { get; }

        public AbstractSublayoutNode(T node, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayouts)
        {
            this.Node = node;
            this.InnerNodeKind = innerNodeKinds;
            this.NodeLayout = nodeLayouts;
            this.Nodes = new List<T>();
            this.RemovedChildren = new List<T>();
        }
    }
    
}

