namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Describes the semantic strength of an exception-flow source.
    /// </summary>
    internal enum ExceptionFlowSourceKind
    {
        /// <summary>
        /// The exception was proven by executable exception-flow analysis.
        /// </summary>
        ProvenException,

        /// <summary>
        /// External XML documentation states that the exception may be
        /// thrown, without proving that behavior for the concrete call site.
        /// </summary>
        ExternalDocumentationEvidence
    }
}
