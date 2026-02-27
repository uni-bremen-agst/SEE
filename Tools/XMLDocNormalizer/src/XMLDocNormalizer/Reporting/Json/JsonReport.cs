using XMLDocNormalizer.Models.Dto;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Represents the complete JSON report produced by the tool.
    /// </summary>
    /// <param name="Tool">The tool name.</param>
    /// <param name="Version">The tool version.</param>
    /// <param name="GeneratedAtUtc">The UTC generation timestamp.</param>
    /// <param name="TargetPath">The target path that was analyzed.</param>
    /// <param name="FindingCount">Total number of findings.</param>
    /// <param name="Findings">The list of findings.</param>
    /// <param name="Metrics">Aggregated run metrics.</param>
    internal sealed record JsonReport(
        string Tool,
        string Version,
        DateTime GeneratedAtUtc,
        string TargetPath,
        int FindingCount,
        List<JsonFindingDto> Findings,
        RunMetricsDto Metrics);
}
