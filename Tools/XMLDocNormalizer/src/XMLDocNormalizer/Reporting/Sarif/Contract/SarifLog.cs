using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// Root SARIF object (SARIF 2.1.0).
    /// </summary>
    /// <param name="Schema">Schema URI used by SARIF tooling.</param>
    /// <param name="Version">SARIF version (must be "2.1.0" for GitHub code scanning).</param>
    /// <param name="Runs">Runs contained in this log.</param>
    internal sealed record SarifLog(
        [property: JsonPropertyName("$schema")] string Schema,
        string Version,
        IReadOnlyList<SarifRun> Runs);
}
