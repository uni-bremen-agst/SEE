namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF region information (1-based coordinates).
    /// </summary>
    /// <param name="StartLine">1-based line number.</param>
    /// <param name="StartColumn">1-based column number.</param>
    /// <param name="Snippet">Optional snippet.</param>
    internal sealed record SarifRegion(
        int StartLine,
        int StartColumn,
        SarifSnippet? Snippet);
}
