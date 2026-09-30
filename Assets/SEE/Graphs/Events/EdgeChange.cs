using System;
using SEE.Graphs.DG;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Graphs.Events
{
    /// <summary>
    /// A change event fired when the state of an edge changed.
    /// </summary>
    public class EdgeChange : ChangeEvent
    {
        /// <summary>
        /// The edge being changed.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// The previous state of the edge before the change.
        /// </summary>
        public readonly State OldState;

        /// <summary>
        /// The new state of the edge after the change.
        /// </summary>
        public readonly State NewState;

        /// <summary>
        /// Constructor for a change of an edge event.
        /// </summary>
        /// <param name="version">The graph version this event is associated to.</param>
        /// <param name="edge">Edge being changed.</param>
        /// <param name="oldState">The old state of the edge.</param>
        /// <param name="newState">The new state of the edge after the change.</param>
        /// <param name="subgraph">The subgraph the edge is contained in.</param>
        public EdgeChange(Guid version, Edge edge, State oldState, State newState, ReflexionSubgraphs subgraph = ReflexionSubgraphs.Architecture) : base(version, subgraph)
        {
            Edge = edge;
            OldState = oldState;
            NewState = newState;
        }

        protected override string Description()
        {
            return $"edge '{Edge.ToShortString()}' changed from {OldState} to {NewState}.";
        }
    }
}