namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF physical location: artifact + region.
    /// </summary>
    /// <param name="ArtifactLocation">Artifact reference (file).</param>
    /// <param name="Region">Region within the file.</param>
    internal sealed record SarifPhysicalLocation(
        SarifArtifactLocation ArtifactLocation,
        SarifRegion Region);
}
