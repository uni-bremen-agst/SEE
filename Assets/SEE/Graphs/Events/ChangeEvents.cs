using System;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Graphs.Events
{
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

        /// <summary>
        /// Returns a string representation of the event, including the name of the class
        /// and the description of the event.
        /// </summary>
        /// <returns>String representation of the event.</returns>
        public override string ToString() => $"{GetType().Name}: {Description()}";

        /// <summary>
        /// Creates a new instance of this change event.
        /// </summary>
        /// <param name="versionId">The unique ID of the graph version this event is associated to.</param>
        /// <param name="affectedGraph">Which graph was affected by this event.</param>
        /// <param name="change">Type of change for this event.</param>
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
        /// <param name="newVersion">The new version to use for the cloned change event.</param>
        /// <returns>Cloned change event with given <paramref name="newVersion"/>.</returns>
        public ChangeEvent CopyWithGuid(Guid newVersion)
        {
            ChangeEvent newChange = (ChangeEvent)MemberwiseClone();
            newChange.VersionId = newVersion;
            return newChange;
        }
    }
}
