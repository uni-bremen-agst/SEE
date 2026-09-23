namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Selects whether exact external metadata references may be discovered
    /// only locally or also through explicitly configured bounded providers.
    /// </summary>
    internal enum ExternalReferenceAcquisitionPolicy
    {
        /// <summary>Only explicit and standardized local artifact sources are used.</summary>
        LocalOnly,

        /// <summary>Local discovery may fall back to bounded remote artifact providers.</summary>
        BoundedRemoteArtifacts
    }
}
