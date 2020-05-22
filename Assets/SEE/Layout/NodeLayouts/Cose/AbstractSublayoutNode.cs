using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public abstract class AbstractSublayoutNode<T>
    {
        /// <summary>
        /// the root node
        /// </summary>
        public T Node { get; }

        /// <summary>
        /// A List with removed children
        /// </summary>
        public List<T> RemovedChildren { get; set; }

        /// <summary>
        /// nodes of the sublayout
        /// </summary>
        public List<T> Nodes { get; set; }

        /// <summary>
        /// the kind of the inner nodes
        /// </summary>
        public InnerNodeKinds InnerNodeKind { get; }

        /// <summary>
        /// the node Layout
        /// </summary>
        public NodeLayouts NodeLayout { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="node">the root node</param>
        /// <param name="innerNodeKinds">the kind of the inner nodes</param>
        /// <param name="nodeLayouts">the node Layout</param>
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

