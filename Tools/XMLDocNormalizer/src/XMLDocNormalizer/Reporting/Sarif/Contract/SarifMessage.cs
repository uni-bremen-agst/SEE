namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF message container.
    /// </summary>
    /// <param name="Text">Message text.</param>
    internal sealed record SarifMessage(string Text);
}
