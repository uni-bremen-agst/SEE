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
        [NonSerialized] private static readonly Dictionary<Node, NodeRef> nodeToNodeRefDict = new();

        /// <summary>
        /// A callback called when a new node value is assigned that differs from null.
        /// </summary>
        /// <param name="node">The node that is assigned to this reference.</param>
        public delegate void ValueIsSet(Node node);

        /// <summary>
        /// Clients can register here to be informed when a new node value is assigned that
        /// differs from null.
        /// </summary>
        public event ValueIsSet OnValueSet;

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
            get => (Node)Elem;
            set
            {
                if (Elem != value)
                {
                    if (Elem != null)
                    {
                        nodeToNodeRefDict.Remove((Node)Elem);
                    }
                    Elem = value;
                    if (Elem != null)
                    {
                        nodeToNodeRefDict[(Node)Elem] = this;
                        OnValueSet?.Invoke(value);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="NodeRef"/> referring to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node whose <see cref="NodeRef"/> is requested.</param>
        /// <returns>The <see cref="NodeRef"/> referring to <paramref name="node"/> or null
        /// if there is none.</returns>
        public static NodeRef Get(Node node)
        {
            Assert.IsNotNull(node);
            return nodeToNodeRefDict[node];
        }

        /// <summary>
        /// Retrieves the <see cref="nodeRef"/> referring to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node whose <see cref="NodeRef"/> is requested.</param>
        /// <param name="nodeRef">The <see cref="NodeRef"/> referring to <paramref name="node"/> or null
        /// if there is none.</param>
        /// <returns>True if a <paramref name="nodeRef"/> corresponding to <paramref name="node"/>
        /// could be found (is this returned value is false, <paramref name="nodeRef"/> will
        /// be null).</returns>
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
        /// <returns>IDs of all incoming and outgoing edges.</returns>
        public ISet<string> GetIdsOfIncomingOutgoingEdges()
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