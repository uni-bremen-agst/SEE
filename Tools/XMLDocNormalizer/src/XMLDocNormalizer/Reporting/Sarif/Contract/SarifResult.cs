namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// Represents a single SARIF result.
    /// </summary>
    /// <param name="RuleId">The rule identifier that produced the result.</param>
    /// <param name="Level">The SARIF severity level.</param>
    /// <param name="Message">The human-readable result message.</param>
    /// <param name="Locations">The locations associated with the result.</param>
    /// <param name="Properties">Additional tool-specific result metadata.</param>
    internal sealed record SarifResult(
        string RuleId,
        string Level,
        SarifMessage Message,
        IReadOnlyList<SarifLocation> Locations,
        IReadOnlyDictionary<string, object?>? Properties = null);
}
