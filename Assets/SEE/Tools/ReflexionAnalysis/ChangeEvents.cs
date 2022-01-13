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
        public readonly Edge edge;
        /// <summary>
        /// The previous state of the edge before the change.
        /// </summary>
        public readonly State oldState;
        /// <summary>
        /// The new state of the edge after the change.
        /// </summary>
        public readonly State newState;

        /// <summary>
        /// Constructor for a change of an edge event.
        /// </summary>
        /// <param name="edge">edge being changed</param>
        /// <param name="old_state">the old state of the edge</param>
        /// <param name="new_state">the new state of the edge after the change</param>
        public EdgeChange(Edge edge, State oldState, State newState)
        {
            this.edge = edge;
            this.oldState = oldState;
            this.newState = newState;
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {edge} changed from {oldState} to {newState}";
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
        public Edge propagatedEdge;

        /// <summary>
        /// Constructor preserving the implementation dependency that is or was
        /// propagated to the architecture graph.
        /// </summary>
        /// <param name="propagatedEdge">the propagated edge</param>
        public PropagatedEdge(Edge propagatedEdge)
        {
            this.propagatedEdge = propagatedEdge;
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
            return base.ToString() + ": new propagated edge " + propagatedEdge.ToString();
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
            return base.ToString() + ": unpropagated edge " + propagatedEdge.ToString();
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
        public Edge mapsToEdge;

        /// <summary>
        /// Constructor preserving the Maps_To edge added to the mapping or removed from it.
        /// </summary>
        /// <param name="mapsToEdge">the Maps_To edge being added or removed</param>
        public MapsToEdge(Edge mapsToEdge)
        {
            this.mapsToEdge = mapsToEdge;
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
            return base.ToString() + ": new Maps_To edge " + mapsToEdge.ToString();
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
            return base.ToString() + ": removed Maps_To edge " + mapsToEdge.ToString();
        }
    }

}