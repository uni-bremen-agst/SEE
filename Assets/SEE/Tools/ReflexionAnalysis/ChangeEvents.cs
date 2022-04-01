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
        public EdgeChange(Edge edge, State oldState, State newState)
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
        public PropagatedEdgeEvent(Edge propagatedEdge, ChangeType change) : base(change)
        {
            PropagatedEdge = propagatedEdge;
        }

        protected override string Description() =>
            $"edge '{PropagatedEdge}' has been {(Change == ChangeType.Removal ? "un" : "")}propagated.";
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was added to the mapping or removed from it.
    /// </summary>
    public class MapsToEdgeEvent : ChangeEvent
    {
        /// <summary>
        /// The Maps_To edge added to the mapping or removed from it.
        /// </summary>
        public readonly Edge MapsToEdge;

        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping or removed from it.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added or removed</param>
        /// <param name="change">the type of change to <paramref name="mapsToEdge"/></param>
        public MapsToEdgeEvent(Edge mapsToEdge, ChangeType change) : base(change)
        {
            MapsToEdge = mapsToEdge;
        }

        protected override string Description() => 
            $"Maps_To edge '{MapsToEdge}' has been {(Change == ChangeType.Addition ? "Added" : "Removed")}.";
    }

    /// <summary>
    /// A change event fired when an edge has been added to or removed from the implementation.
    /// </summary>
    public class ImplementationEdgeEvent : ChangeEvent
    {
        /// <summary>
        /// The edge added to or removed from the Implementation.
        /// </summary>
        public readonly Edge ImplementationEdge;

        /// <summary>
        /// Constructor preserving the edge added to or removed from the implementation.
        /// </summary>
        /// <param name="implementationEdge">the implementation edge being added or removed</param>
        /// <param name="change">the type of change to <paramref name="implementationEdge"/></param>
        public ImplementationEdgeEvent(Edge implementationEdge, ChangeType change) : base(change)
        {
            ImplementationEdge = implementationEdge;
        }

        protected override string Description() => 
            $"implementation edge '{ImplementationEdge}' has been "
            + $"{(Change == ChangeType.Addition ? "added to" : "removed from")} the graph.";
    }
    
    /// <summary>
    /// A change event fired when an edge has been removed from or added to the architecture.
    /// </summary>
    public class ArchitectureEdgeEvent : ChangeEvent
    {
        /// <summary>
        /// The edge removed from or added to the architecture.
        /// </summary>
        public readonly Edge ArchitectureEdge;

        /// <summary>
        /// Constructor preserving the edge removed from or added to the architecture.
        /// </summary>
        /// <param name="architectureEdge">the architecture edge being removed or added</param>
        /// <param name="change">the type of change to <paramref name="architectureEdge"/></param>
        public ArchitectureEdgeEvent(Edge architectureEdge, ChangeType change) : base(change)
        {
            ArchitectureEdge = architectureEdge;
        }

        protected override string Description() => 
            $"architecture edge '{ArchitectureEdge}' has been "
            + $"{(Change == ChangeType.Addition ? "added to" : "removed from")} the graph.";
    }

    /// <summary>
    /// A change event fired when a node is added or removed as a child.
    /// </summary>
    public abstract class HierarchyChangeEvent : ChangeEvent
    {
        /// <summary>
        /// The parent node, having <see cref="Child"/> as its direct child.
        /// </summary>
        public readonly Node Parent;
        
        /// <summary>
        /// The child node, being a direct descendant of <see cref="Parent"/>.
        /// </summary>
        public readonly Node Child;

        protected HierarchyChangeEvent(Node parent, Node child, ChangeType change) : base(change)
        {
            Parent = parent;
            Child = child;
        }
    }

    /// <summary>
    /// A change event fired when an implementation node is added or removed as a child.
    /// </summary>
    public class ImplementationHierarchyChangeEvent : HierarchyChangeEvent
    {
        protected override string Description() => 
            $"implementation node '{Child}'"
            + $" {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent}'";

        public ImplementationHierarchyChangeEvent(Node parent, Node child, ChangeType change) 
            : base(parent, child, change)
        {
            // Nothing remains to be done.
        }
    }
    
    /// <summary>
    /// A change event fired when an architecture node is added or removed as a child.
    /// </summary>
    public class ArchitectureHierarchyChangeEvent : HierarchyChangeEvent
    {
        protected override string Description() => 
            $"architecture node '{Child}'"
            + $" {(Change == ChangeType.Addition ? "added as child to" : "removed as child from")} parent '{Parent}'";

        public ArchitectureHierarchyChangeEvent(Node parent, Node child, ChangeType change) 
            : base(parent, child, change)
        {
            // Nothing remains to be done.
        }
    }
    
}