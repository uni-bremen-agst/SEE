namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if the hierarchy is not a tree structure, i.e., if it contains cycles.
    /// </summary>
    public class CyclicHierarchyException : ArchitectureAnalysisException
    {
        /// <summary>
        /// Constructs a new <see cref="CyclicHierarchyException"/>.
        /// </summary>
        public CyclicHierarchyException() : base("The hierarchy must be a tree, that is, no cycles may exist!")
        {
        }
    }
}
