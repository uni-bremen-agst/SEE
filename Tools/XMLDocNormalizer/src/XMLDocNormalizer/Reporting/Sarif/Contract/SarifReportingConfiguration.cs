namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// Default reporting configuration for a SARIF rule.
    /// </summary>
    /// <param name="Level">SARIF level: "error", "warning", or "note".</param>
    internal sealed record SarifReportingConfiguration(string Level);
}
