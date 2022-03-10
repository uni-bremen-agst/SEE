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

        public override string ToString()
        {
            return $"{base.ToString()}: {Edge} changed from {OldState} to {NewState}";
        }
    }

    /// <summary>
    /// A change event fired when an implementation dependency was either propagated to
    /// the architecture or unpropagated from the architecture.
    /// </summary>
    public abstract class PropagatedEdge : ChangeEvent
    {
        /// <summary>
        /// The implementation dependency propagated from the implementation to the architecture.
        /// </summary>
        public readonly Edge ThePropagatedEdge;

        /// <summary>
        /// Constructor preserving the implementation dependency that is or was
        /// propagated to the architecture graph.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        public PropagatedEdge(Edge propagatedEdge)
        {
            ThePropagatedEdge = propagatedEdge;
        }
    }

    /// <summary>
    /// A change event fired when an implementation dependency was propagated to
    /// the architecture.
    ///
    /// Note: This event is fired only once for the very first time a corresponding
    /// new propagated edge was created in the architecture. If there is already such
    /// a propagated edge in the architecture, this existing edge is re-used and only
    /// its counter is updated.
    /// </summary>
    public class PropagatedEdgeAdded : PropagatedEdge
    {
        /// <summary>
        /// Constructor preserving the implementation dependency propagated from the
        /// implementation to the architecture.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        public PropagatedEdgeAdded(Edge propagatedEdge) : base(propagatedEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": new propagated edge " + ThePropagatedEdge;
        }
    }

    /// <summary>
    /// A change event fired when a propagated dependency edge was removed from the architecture.
    /// </summary>
    public class PropagatedEdgeRemoved : PropagatedEdge
    {
        /// <summary>
        /// Constructor passing on the edge to be removed.
        /// </summary>
        /// <param name="propagatedEdge">edge to be removed</param>
        public PropagatedEdgeRemoved(Edge propagatedEdge) : base(propagatedEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": unpropagated edge " + ThePropagatedEdge;
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was added to the mapping or removed from it.
    /// </summary>
    public abstract class MapsToEdge : ChangeEvent
    {
        /// <summary>
        /// The Maps_To edge added to the mapping or removed from it.
        /// </summary>
        public readonly Edge TheMapsToEdge;

        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping or removed from it.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added or removed</param>
        public MapsToEdge(Edge mapsToEdge)
        {
            TheMapsToEdge = mapsToEdge;
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was added to the mapping.
    /// </summary>
    public class MapsToEdgeAdded : MapsToEdge
    {
        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added</param>
        public MapsToEdgeAdded(Edge mapsToEdge) : base(mapsToEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": new Maps_To edge " + TheMapsToEdge;
        }
    }

    /// <summary>
    /// A change event fired when a Maps_To edge was removed from the mapping.
    /// </summary>
    public class MapsToEdgeRemoved : MapsToEdge
    {
        /// <summary>
        /// Constructor preserving the Maps_To edge removed from the mapping.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being removed</param>
        public MapsToEdgeRemoved(Edge mapsToEdge) : base(mapsToEdge)
        {
        }

        public override string ToString()
        {
            return base.ToString() + ": removed Maps_To edge " + TheMapsToEdge;
        }
    }

    /// <summary>
    /// A change event fired when an edge has been removed from the implementation.
    /// </summary>
    public class ImplementationEdgeRemoved : ChangeEvent
    {
        /// <summary>
        /// The edge removed from the Implementation.
        /// </summary>
        public readonly Edge RemovedEdge;

        /// <summary>
        /// Constructor preserving the edge removed from the implementation.
        /// </summary>
        /// <param name="removedEdge">the implementation edge being removed</param>
        public ImplementationEdgeRemoved(Edge removedEdge)
        {
            RemovedEdge = removedEdge;
        }

        public override string ToString()
        {
            return base.ToString() + $": implementation edge '{RemovedEdge}' has been removed from the graph.";
        }
    }
}