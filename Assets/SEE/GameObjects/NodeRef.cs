using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine.Assertions;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a graph node that can be attached to a game object as a component.
    /// In addition, a mapping of <see cref="Node"/>s onto <see cref="NodeRef"/>s is
    /// maintained.
    /// </summary>
    public class NodeRef : GraphElementRef
    {
        /// <summary>
        /// Maps a Node onto its NodeRef (the one referring to it).
        /// </summary>
        [NonSerialized] private static readonly Dictionary<Node, NodeRef> nodeToNodeRefDict = new Dictionary<Node, NodeRef>();

        /// <summary>
        /// The graph node this node reference is referring to, that is, is visualized
        /// by this game object.
        ///
        /// As a side effect of assigning <see cref="Value"/>, <see cref="nodeToNodeRefDict"/>
        /// will be updated, that is, the mapping of a <see cref="Node"/> onto the
        /// <see cref="NodeRef"/> referring to this <see cref="Node"/>, which can be
        /// retrieved by <see cref="Get(Node)"/>.
        ///
        /// Note: <see cref="Value"/> will not be serialized to prevent duplicating and
        /// endless serialization by both Unity and Odin.
        /// </summary>
        public Node Value
        {
            get => (Node)elem;
            set
            {
                if (elem != value)
                {
                    if (elem != null)
                    {
                        nodeToNodeRefDict.Remove((Node)elem);
                    }
                    elem = value;
                    if (elem != null)
                    {
                        nodeToNodeRefDict[(Node)elem] = this;
                    }
                }
            }
        }

        /// <summary>
        /// Sets this <see cref="NodeRef"/> to referring to <paramref name="node"/>.
        ///
        /// Unlike, setting <see cref="Value"/>, this assignment will not alter the
        /// mapping of <see cref="Node"/>s onto <see cref="NodeRef"/>s, in other words,
        /// <see cref="nodeToNodeRefDict"/>. That is, the result of <see cref="Get(Node)"/>
        /// does not depend upon this assignment.
        /// </summary>
        /// <param name="node">the graph node this <see cref="NodeRef"/> should be
        /// referring to</param>
        public void SetNode(Node node)
        {
            elem = node;
        }

        /// <summary>
        /// Returns the <see cref="NodeRef"/> referring to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose <see cref="NodeRef"/> is requested</param>
        /// <returns>the <see cref="NodeRef"/> referring to <paramref name="node"/> or null
        /// if there is none</returns>
        public static NodeRef Get(Node node)
        {
            Assert.IsNotNull(node);
            return nodeToNodeRefDict[node];
        }

        /// <summary>
        /// Retrieves the <see cref="nodeRef"/> referring to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose <see cref="NodeRef"/> is requested</param>
        /// <param name="nodeRef">the <see cref="NodeRef"/> referring to <paramref name="node"/> or null
        /// if there is none</param>
        /// <returns>true if a <paramref name="nodeRef"/> corresponding to <paramref name="node"/>
        /// could be found (is this returned value is false, <paramref name="nodeRef"/> will
        /// be null)</returns>
        public static bool TryGet(Node node, out NodeRef nodeRef)
        {
            bool result = false;
            nodeRef = null;
            if (nodeToNodeRefDict.TryGetValue(node, out NodeRef v))
            {
                result = true;
                nodeRef = v;
            }
            return result;
        }

        /// <summary>
        /// Returns the IDs of all incoming and outgoing edges for this NodeRef.
        /// </summary>
        /// <returns>IDs of all incoming and outgoing edges</returns>
        public ISet<string> GetEdgeIds()
        {
            HashSet<string> edgeIDs = new HashSet<string>();
            foreach (Edge edge in Value.Outgoings)
            {
                edgeIDs.Add(edge.ID);
            }
            foreach (Edge edge in Value.Incomings)
            {
                edgeIDs.Add(edge.ID);
            }
            return edgeIDs;
        }
    }
}