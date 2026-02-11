namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// A single SARIF result (one finding instance).
    /// </summary>
    /// <param name="RuleId">Rule id that produced the result.</param>
    /// <param name="Level">SARIF level.</param>
    /// <param name="Message">Human-readable message.</param>
    /// <param name="Locations">Locations associated with the result.</param>
    internal sealed record SarifResult(
        string RuleId,
        string Level,
        SarifMessage Message,
        IReadOnlyList<SarifLocation> Locations);
}
