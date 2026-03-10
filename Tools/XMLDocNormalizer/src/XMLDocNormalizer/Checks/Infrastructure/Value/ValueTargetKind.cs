namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Classifies the member kind for value-tag analysis.
    /// </summary>
    internal enum ValueTargetKind
    {
        ReadableProperty,
        WriteOnlyProperty,
        Indexer,
        InvalidMember
    }
}