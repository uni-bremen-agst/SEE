using System;
using SEE.Graphs;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Graphs.Events
{
    /// <summary>
    /// An event fired when a node is added to or removed from the graph.
    /// Note that this event will never originate from the node which was added or removed; it will instead be emitted
    /// from the graph it is (or was) contained in.
    /// </summary>
    public class NodeEvent : GraphEvent
    {
        /// <summary>
        /// The node which has either been added to or deleted from the graph.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Creates a new node event, representing the addition or removal of <paramref name="node"/> to/from the graph.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="node">The node which has either been added to or deleted from the graph.</param>
        /// <param name="change">The type of change to the node.</param>
        /// <param name="affectedGraph">The graph the node change is associated to.</param>
        /// <exception cref="ArgumentException">Thrown when attempting to change a mapping or full reflexion hierarchy.</exception>
        public NodeEvent(Guid version, Node node, ChangeType change, ReflexionSubgraphs? affectedGraph = null)
            : base(version, affectedGraph ?? node.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraphs.Mapping || affectedGraph == ReflexionSubgraphs.FullReflexion)
            {
                throw new ArgumentException("Nodes can only be added to architecture or implementation!");
            }

            Node = node;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the node, and the type of change.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() => $"node '{Node.ToShortString()}' {(Change == ChangeType.Addition ? "added to" : "removed from")} {Affected.ToShortString()}";
    }
}
