namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF tool container.
    /// </summary>
    /// <param name="Driver">The tool driver component.</param>
    internal sealed record SarifTool(SarifToolComponent Driver);
}
