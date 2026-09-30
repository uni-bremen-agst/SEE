using System;
using SEE.Graphs;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Graphs.Events
{
    /// <summary>
    /// An event fired when a node is added or removed as a child.
    /// Note that this event will never be emitted from the <see cref="Parent"/>, only from the <see cref="Child"/>
    /// or its graph.
    /// </summary>
    public class HierarchyEvent : GraphEvent
    {
        /// <summary>
        /// The parent node, having <see cref="Child"/> as its direct child.
        /// </summary>
        public readonly Node Parent;

        /// <summary>
        /// The child node, being a direct descendant of <see cref="Parent"/>.
        /// </summary>
        public readonly Node Child;

        /// <summary>
        /// Creates a new hierarchy event, representing the addition or removal of <paramref name="child"/>
        /// as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="parent">The parent node.</param>
        /// <param name="child">The child node.</param>
        /// <param name="change">The type of change to the hierarchy.</param>
        /// <param name="affectedGraph">The graph the hierarchy change is associated to.</param>
        /// <exception cref="ArgumentException">Thrown when attempting to change a mapping or full reflexion hierarchy.</exception>
        public HierarchyEvent(Guid version, Node parent, Node child, ChangeType change, ReflexionSubgraphs? affectedGraph = null) : base(version, affectedGraph ?? child.GetSubgraph(), change)
        {
            if (affectedGraph == ReflexionSubgraphs.Mapping || affectedGraph == ReflexionSubgraphs.FullReflexion)
            {
                throw new ArgumentException("Only architecture or implementation hierarchy can be changed!");
            }

            Parent = parent;
            Child = child;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the parent
        /// and child nodes, and the type of change.
        /// </summary>
        /// <returns>A string description of this event.</returns>
        protected override string Description() =>
            $"{Affected.ToShortString()} node '{Child.ToShortString()}' {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent.ToShortString()}'";
    }
}
