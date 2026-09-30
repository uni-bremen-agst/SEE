using System;
using SEE.Graphs.Reflexion;

namespace SEE.Graphs.Events
{
    /// <summary>
    /// An event representing a change to a graph component.
    /// May be used outside of reflexion analysis contexts, in which case
    /// the <see cref="ReflexionSubgraphs"/> will be None.
    /// </summary>
    public abstract class GraphEvent : ChangeEvent
    {
        /// <summary>
        /// Creates a new instance of this graph event.
        /// </summary>
        /// <param name="version">The version ID associated with this event.</param>
        /// <param name="affectedGraph">The graph affected by this event.</param>
        /// <param name="change">The type of change this event represents.</param>
        protected GraphEvent(Guid version, ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(version, affectedGraph, change)
        {
        }
    }
}
