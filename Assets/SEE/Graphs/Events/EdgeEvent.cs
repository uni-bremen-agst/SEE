using System;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.DataModel.Events
{
    /// <summary>
    /// An event fired when an edge was added to the graph or removed from it.
    /// Note that this event will never originate from the edge which was changed; it will instead be emitted
    /// from the graph it is contained in.
    /// </summary>
    public class EdgeEvent : GraphEvent
    {
        /// <summary>
        /// The edge added to the graph or removed from it.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructor preserving the edge added to the graph or removed from it.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="edge">The edge being added or removed.</param>
        /// <param name="change">The type of change to <paramref name="edge"/>.</param>
        /// <param name="affectedGraph">The graph the edge was added to or removed from.</param>
        public EdgeEvent(Guid version, Edge edge, ChangeType change, ReflexionSubgraphs? affectedGraph = null)
            : base(version, affectedGraph ?? edge.GetSubgraph(), change)
        {
            Edge = edge;
        }

        /// <summary>
        /// Returns a string description of this event, including the affected graph, the edge, and the type of change.
        /// </summary>
        /// <returns>A string describing the edge event.</returns>
        protected override string Description() =>
            $"{Affected.ToShortString()} edge '{Edge.ToShortString()}' has been {(Change == ChangeType.Addition ? "Added" : "Removed")}.";
    }
}
