namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF location wrapper.
    /// </summary>
    /// <param name="PhysicalLocation">Physical location information.</param>
    internal sealed record SarifLocation(SarifPhysicalLocation PhysicalLocation);
}
