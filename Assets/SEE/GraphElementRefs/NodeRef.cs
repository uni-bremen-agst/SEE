using System.Collections.Generic;
using SEE.DataModel.DG;

namespace SEE.GraphElementRefs
{
    /// <summary>
    /// A reference to a graph node that can be attached to a game object as a component.
    /// </summary>
    public class NodeRef : GraphElementRef
    {
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
                    Elem = value;
                    if (Elem != null)
                    {
                        OnValueSet?.Invoke(value);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the IDs of all incoming and outgoing edges for this NodeRef.
        /// </summary>
        /// <returns>IDs of all incoming and outgoing edges.</returns>
        public ISet<string> GetIdsOfIncomingOutgoingEdges()
        {
            HashSet<string> edgeIDs = new();
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
