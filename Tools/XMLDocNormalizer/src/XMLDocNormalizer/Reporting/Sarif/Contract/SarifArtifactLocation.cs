namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF artifact location (file reference).
    /// </summary>
    /// <param name="Uri">URI for the artifact, typically a relative file path.</param>
    internal sealed record SarifArtifactLocation(string Uri);
}
