using System;

namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Type of a reflexion subgraph.
    /// </summary>
    [Flags]
    public enum ReflexionSubgraphs
    {
        /// <summary>
        /// No reflexion subgraph.
        /// </summary>
        None = 0,

        /// <summary>
        /// The implementation graph.
        /// </summary>
        Implementation = 1 << 0,

        /// <summary>
        /// The architecture graph.
        /// </summary>
        Architecture = 1 << 1,

        /// <summary>
        /// The mapping graph.
        /// </summary>
        Mapping = 1 << 2,

        /// <summary>
        /// The full reflexion graph.
        /// </summary>
        FullReflexion = 1 << 3
    }
}
