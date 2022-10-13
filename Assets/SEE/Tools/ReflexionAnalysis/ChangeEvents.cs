using System;
using SEE.DataModel.DG;

namespace SEE.Tools.ReflexionAnalysis
{

    /// <summary>
    /// Type of change to a graph element (node or edge, including "part-of" edges).
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// The graph element has been added.
        /// </summary>
        Addition,
        
        /// <summary>
        /// The graph element has been removed.
        /// </summary>
        Removal
    }

    /// <summary>
    /// The event information about the change of the state of the observed subject.
    /// This class is intended to be specialized for more specific change events.
    /// </summary>
    public abstract class ChangeEvent
    {
        /// <summary>
        /// Type of change for this event, i.e., whether the relevant graph element has been added or removed.
        ///
        /// May be null if not applicable to this type.
        /// </summary>
        public readonly ChangeType? Change;

        /// <summary>
        /// Which graph was affected by this event.
        ///
        /// Note that this only counts towards direct changes—e.g., a <see cref="MapsToEdgeEvent"/> will have this
        /// attribute set to <see cref="ReflexionSubgraph.Mapping"/>, even though the architecture graph may be affected
        /// as well due to changes to its propagated edges.
        /// 
        /// If an event can't be clearly traced to a single subgraph, this attribute will be set to
        /// <see cref="ReflexionSubgraph.FullReflexion"/>.
        /// </summary>
        public readonly ReflexionSubgraph Affected;
        
        /// <summary>
        /// A textual representation of the event.
        /// Must be human-readable, distinguishable from other <see cref="ChangeEvent"/>s, and
        /// contain all relevant information about the event.
        /// The name of the class needn't be included, as <see cref="ToString"/> will contain it.
        /// </summary>
        /// <returns>Textual representation of the event.</returns>
        protected abstract string Description();

        public override string ToString() => $"{GetType().Name}: {Description()}";

        protected ChangeEvent(ReflexionSubgraph affectedGraph, ChangeType? change = null)
        {
            Change = change;
            Affected = affectedGraph;
        }
    }
    
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
        public EdgeChange(Edge edge, State oldState, State newState) : base(ReflexionSubgraph.Architecture)
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
        public PropagatedEdgeEvent(Edge propagatedEdge, ChangeType change) : base(ReflexionSubgraph.Architecture, change)
        {
            PropagatedEdge = propagatedEdge;
        }

        protected override string Description() =>
            $"edge '{PropagatedEdge.ToShortString()}' has been {(Change == ChangeType.Removal ? "un" : "")}propagated.";
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
        public EdgeEvent(Edge edge, ChangeType change, ReflexionSubgraph affectedGraph) : base(affectedGraph, change)
        {
            Edge = edge;
        }

        protected override string Description() => 
            $"{Affected} edge '{Edge.ToShortString()}' has been {(Change == ChangeType.Addition ? "Added" : "Removed")}.";
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

        public HierarchyChangeEvent(Node parent, Node child, ChangeType change, ReflexionSubgraph affectedGraph) : base(affectedGraph, change)
        {
            if (affectedGraph == ReflexionSubgraph.Mapping || affectedGraph == ReflexionSubgraph.FullReflexion)
            {
                throw new ArgumentException("Only architecture or implementation hierarchy can be changed!");
            }
            Parent = parent;
            Child = child;
        }

        protected override string Description() => 
            $"{Affected} node '{Child.ToShortString()}' {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent.ToShortString()}'";
    }

    /// <summary>
    /// A change event fired when a node is added to or removed from the graph.
    /// </summary>
    public class NodeChangeEvent : ChangeEvent
    {
        /// <summary>
        /// The node which has either been added to or deleted from the graph.
        /// </summary>
        public readonly Node Node;

        public NodeChangeEvent(Node node, ChangeType change, ReflexionSubgraph affectedGraph) : base(affectedGraph, change)
        {
            if (affectedGraph != ReflexionSubgraph.Architecture && affectedGraph != ReflexionSubgraph.Implementation)
            {
                throw new ArgumentException("Nodes can only be added to architecture or implementation!");
            }
            Node = node;
        }

        protected override string Description() => $"node '{Node.ToShortString()}' {(Change == ChangeType.Addition ? "added to" : "removed from")} {Affected}";
    }
}