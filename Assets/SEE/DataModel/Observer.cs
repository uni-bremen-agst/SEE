using System.Collections.Generic;
using System.Linq;
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
    /// Common interface of all observers of instances of Observable.
    /// </summary>
    public interface Observer
    {
        /// <summary>
        /// This method is intended to be called by the Observable when its state has
        /// changed. The given parameter gives more details about the change.
        /// </summary>
        /// <param name="changeEvent">details about the change of the state</param>
        void Update(ChangeEvent changeEvent);
    }
}