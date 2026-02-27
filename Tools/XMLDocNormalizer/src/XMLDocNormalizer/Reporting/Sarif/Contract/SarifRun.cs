namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// A single SARIF run.
    /// </summary>
    /// <param name="Tool">Tool metadata.</param>
    /// <param name="Results">Results produced by the tool.</param>
    /// <param name="Properties">Tool-specific run properties.</param>
    internal sealed record SarifRun(
        SarifTool Tool,
        IReadOnlyList<SarifResult> Results,
        Dictionary<string, object>? Properties = null);
}
