namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF rule definition, referenced by results via rule id.
    /// </summary>
    /// <param name="Id">Stable rule id.</param>
    /// <param name="ShortDescription">Short placeholder-free description of the rule.</param>
    /// <param name="FullDescription">Longer placeholder-free description of the rule.</param>
    /// <param name="DefaultConfiguration">Default reporting configuration.</param>
    internal sealed record SarifRule(
        string Id,
        SarifMultiformatMessageString ShortDescription,
        SarifMultiformatMessageString FullDescription,
        SarifReportingConfiguration? DefaultConfiguration);
}
