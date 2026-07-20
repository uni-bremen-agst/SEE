namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Classifies the member kind for value-tag analysis.
    /// </summary>
    internal enum ValueTargetKind
    {
        /// <summary>
        /// Indicates a readable property.
        /// </summary>
        ReadableProperty,

        /// <summary>
        /// Indicates a write-only property.
        /// </summary>
        WriteOnlyProperty,

        /// <summary>
        /// Indicates an indexer.
        /// </summary>
        Indexer,

        /// <summary>
        /// Indicates a member that is not a valid value-tag target.
        /// </summary>
        InvalidMember
    }
}