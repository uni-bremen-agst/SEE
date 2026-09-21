namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Identifies how validated external source bytes were supplied without
    /// assigning trust or identity semantics to that origin.
    /// </summary>
    internal enum ExternalSourceMaterialOrigin
    {
        /// <summary>
        /// The bytes came from validated Embedded Source provenance retained
        /// by P4B.
        /// </summary>
        Embedded,

        /// <summary>
        /// The bytes came from an explicit caller-owned stream.
        /// </summary>
        ExplicitStream,

        /// <summary>
        /// The bytes came from an explicitly selected caller path.
        /// </summary>
        ExplicitFile,

        /// <summary>
        /// The bytes came from a candidate projected inside an explicitly
        /// configured local source root.
        /// </summary>
        LocalMapping,

        /// <summary>
        /// The bytes came from an explicitly enabled Source Link request.
        /// </summary>
        SourceLink
    }
}
