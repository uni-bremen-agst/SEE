namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Defines a non-parallel test collection for semantic exception-flow tests.
    /// </summary>
    /// <remarks>
    /// These tests create temporary cross-project workspaces and depend on project reference
    /// resolution. Running them concurrently with unrelated tests can make the referenced
    /// project graph unavailable or incomplete on some machines.
    /// </remarks>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class SemanticExceptionTestCollection
    {
        /// <summary>
        /// The name of the semantic exception test collection.
        /// </summary>
        public const string Name = "Semantic exception tests";
    }
}
