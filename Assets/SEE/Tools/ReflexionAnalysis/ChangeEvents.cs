using System;
using SEE.DataModel;
using SEE.DataModel.DG;

namespace SEE.Tools.ReflexionAnalysis
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
        /// <param name="edge">edge being changed</param>
        /// <param name="old_state">the old state of the edge</param>
        /// <param name="new_state">the new state of the edge after the change</param>
        public EdgeChange(Edge edge, State oldState, State newState) : base(AffectedGraph.Architecture)
        {
            Edge = edge;
            OldState = oldState;
            NewState = newState;
        }

        protected override string Description()
        {
            return $"edge '{Edge}' changed from {OldState} to {NewState}.";
        }
    }

    /// <summary>
    /// A change event fired when an implementation dependency was either propagated to
    /// the architecture or unpropagated from the architecture.
    /// </summary>
    public class PropagatedEdgeEvent : ChangeEvent
    {
        /// <summary>
        /// The implementation dependency propagated from the implementation to the architecture.
        /// </summary>
        public readonly Edge PropagatedEdge;

        /// <summary>
        /// Constructor preserving the implementation dependency that is or was
        /// propagated to the architecture graph.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        /// <param name="change">the type of change to <paramref name="propagatedEdge"/></param>
        public PropagatedEdgeEvent(Edge propagatedEdge, ChangeType change) : base(AffectedGraph.FullReflexion, change)
        {
            PropagatedEdge = propagatedEdge;
        }

        protected override string Description() =>
            $"edge '{PropagatedEdge}' has been {(Change == ChangeType.Removal ? "un" : "")}propagated.";
    }

    /// <summary>
    /// A change event fired when an edge was added to the reflexion graph or removed from it.
    /// The specific graph type it was added to/removed from is stored in <see cref="Affected"/>.
    /// </summary>
    public class EdgeEvent : ChangeEvent
    {
        /// <summary>
        /// The edge added to the graph or removed from it.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructor preserving the edge added to the graph or removed from it.
        /// </summary>
        /// <param name="edge">the edge being added or removed</param>
        /// <param name="change">the type of change to <paramref name="edge"/></param>
        /// <param name="affectedGraph">The graph the edge was added to or removed from</param>
        public EdgeEvent(Edge edge, ChangeType change, AffectedGraph affectedGraph) : base(affectedGraph, change)
        {
            Edge = edge;
        }

        protected override string Description() => 
            $"{Affected} edge '{Edge}' has been {(Change == ChangeType.Addition ? "Added" : "Removed")}.";
    }

    /// <summary>
    /// A change event fired when a node is added or removed as a child.
    /// </summary>
    public class HierarchyChangeEvent : ChangeEvent
    {
        /// <summary>
        /// The parent node, having <see cref="Child"/> as its direct child.
        /// </summary>
        public readonly Node Parent;
        
        /// <summary>
        /// The child node, being a direct descendant of <see cref="Parent"/>.
        /// </summary>
        public readonly Node Child;

        public HierarchyChangeEvent(Node parent, Node child, ChangeType change, AffectedGraph affectedGraph) : base(affectedGraph, change)
        {
            if (affectedGraph == AffectedGraph.Mapping || affectedGraph == AffectedGraph.FullReflexion)
            {
                throw new ArgumentException("Only architecture or implementation hierarchy can be changed!");
            }
            Parent = parent;
            Child = child;
        }

        protected override string Description() => 
            $"{Affected} node '{Child}' {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent}'";
    }
}