using System;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.DataModel
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
        /// attribute set to <see cref="ReflexionSubgraphs.Mapping"/>, even though the architecture graph may be affected
        /// as well due to changes to its propagated edges.
        ///
        /// If an event can't be clearly traced to a single subgraph, this attribute will be set to
        /// <see cref="ReflexionSubgraphs.FullReflexion"/>.
        /// If an event did not occur in the context of the reflexion analysis, this attribute will be set to
        /// <see cref="ReflexionSubgraphs.None"/>.
        /// </summary>
        public readonly ReflexionSubgraphs Affected;

        /// <summary>
        /// Unique ID of the graph version this event is associated to.
        /// </summary>
        public Guid VersionId { get; private set; }

        /// <summary>
        /// A textual representation of the event.
        /// Must be human-readable, distinguishable from other <see cref="ChangeEvent"/>s, and
        /// contain all relevant information about the event.
        /// The name of the class needn't be included, as <see cref="ToString"/> will contain it.
        /// </summary>
        /// <returns>Textual representation of the event.</returns>
        protected abstract string Description();

        public override string ToString() => $"{GetType().Name}: {Description()}";

        protected ChangeEvent(Guid versionId, ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
        {
            VersionId = versionId;
            Change = change;
            Affected = affectedGraph ?? ReflexionSubgraphs.None;
        }

        /// <summary>
        /// Creates a new instance of this change event (using a shallow memberwise clone)
        /// with the given <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="newVersion">The new version to use for the cloned change event</param>
        /// <returns>cloned change event with given <paramref name="newVersion"/></returns>
        public ChangeEvent CopyWithGuid(Guid newVersion)
        {
            ChangeEvent newChange = (ChangeEvent)MemberwiseClone();
            newChange.VersionId = newVersion;
            return newChange;
        }
    }

    /// <summary>
    /// A change event fired when a node is is implicitly mapped or unmapped.
    /// </summary>
    public class MapsToChange : ChangeEvent
    {
        /// <summary>
        /// The source node which is mapped or unmapped.
        /// </summary>
        public Node Source { get; }

        /// <summary>
        /// The target node from which the origin node is removed or mapped to.
        /// </summary>
        public Node Target { get; }

        /// <summary>
        /// Constructor for a maps to change event.
        /// </summary>
        /// <param name="versionId">the graph version this event is associated to</param>
        /// <param name="source">The source node which is mapped or unmapped</param>
        /// <param name="target">The target node from which the origin node is removed or mapped to</param>
        /// <param name="affectedGraph">the graph version this event is associated to</param>
        /// <param name="change">If the source node was mapped or unmapped.</param>
        public MapsToChange(Guid versionId,
                            Node source,
                            Node target, 
                            ReflexionSubgraphs? affectedGraph = null, 
                            ChangeType? change = null) : base(versionId, affectedGraph, change)
        { 
            this.Source = source;
            this.Target = target;
        }

        protected override string Description()
        {
            string operation = Change == ChangeType.Addition ? "mapped to" : "removed from";
            string description = $"Node '{Source.ID}' was {operation} {Target.ID}.";
            return description;
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
        /// <param name="version">the graph version this event is associated to</param>
        /// <param name="edge">edge being changed</param>
        /// <param name="oldState">the old state of the edge</param>
        /// <param name="newState">the new state of the edge after the change</param>
        /// <param name="subgraph">the subgraph the edge is contained in</param>
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