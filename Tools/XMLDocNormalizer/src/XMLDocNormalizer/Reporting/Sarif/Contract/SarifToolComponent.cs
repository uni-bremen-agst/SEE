namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF tool component (driver).
    /// </summary>
    /// <param name="Name">Tool name.</param>
    /// <param name="Version">Tool version.</param>
    /// <param name="Rules">Rules referenced by results.</param>
    internal sealed record SarifToolComponent(
        string Name,
        string? Version,
        IReadOnlyList<SarifRule> Rules);
}
