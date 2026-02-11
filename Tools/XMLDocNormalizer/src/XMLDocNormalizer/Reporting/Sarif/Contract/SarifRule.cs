namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF rule definition, referenced by results via <see cref="SarifResult.RuleId"/>.
    /// </summary>
    /// <param name="Id">Stable rule id (e.g. "DOC200").</param>
    /// <param name="ShortDescription">Short description of the rule.</param>
    /// <param name="DefaultConfiguration">Default reporting configuration.</param>
    internal sealed record SarifRule(
        string Id,
        SarifMultiformatMessageString ShortDescription,
        SarifReportingConfiguration? DefaultConfiguration);
}
