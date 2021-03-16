using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine.Assertions;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph node that can be attached to a game object as a component.
    /// </summary>
    public class NodeRef : GraphElementRef
    {
        /// <summary>
        /// Maps a Node onto its NodeRef (the one referring to it).
        /// </summary>
        [NonSerialized] private static Dictionary<Node, NodeRef> nodeToNodeRefDict = new Dictionary<Node, NodeRef>();

        /// <summary>
        /// The graph node this node reference is referring to. It will be set either
        /// by a graph renderer while in editor mode or at runtime by way of an
        /// AbstractSEECity object.
        /// It will not be serialized to prevent duplicating and endless serialization
        /// by both Unity and Odin.
        /// </summary>
        [NonSerialized] private Node node;
        public Node Value
        {
            get => node;
            set
            {
                if (node != value)
                {
                    if (node != null)
                    {
                        nodeToNodeRefDict.Remove(node);
                    }
                    node = value;
                    if (node != null)
                    {
                        nodeToNodeRefDict[node] = this;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the NodeRef referring to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose NodeRef is requested</param>
        /// <returns>the NodeRef referring to <paramref name="node"/> or null if there is none</returns>
        public static NodeRef Get(Node node)
        {
            Assert.IsNotNull(node);
            return nodeToNodeRefDict[node];
        }
    }
}