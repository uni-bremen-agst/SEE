namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Provides standardized Portable PDB source-document language identifiers.
    /// </summary>
    internal static class ExternalSourceDocumentLanguageIdentifiers
    {
        /// <summary>
        /// Gets the C# source-document language identifier emitted by Roslyn.
        /// </summary>
        /// <value>The standardized Portable PDB C# language GUID.</value>
        public static Guid CSharp { get; } =
            new("3f5162f8-07c6-11d3-9053-00c04fa302a1");
    }
}
