namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Classifies how a property behaves with respect to value documentation.
    /// </summary>
    internal enum PropertyValueKind
    {
        Readable,
        WriteOnly,
        Other
    }
}