namespace XMLDocNormalizer.Reporting.Sarif.Contract
{
    /// <summary>
    /// SARIF snippet container.
    /// </summary>
    /// <param name="Text">Snippet text.</param>
    internal sealed record SarifSnippet(string Text);
}
