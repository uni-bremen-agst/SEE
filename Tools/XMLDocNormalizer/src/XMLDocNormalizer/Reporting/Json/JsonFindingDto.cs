using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Represents a single finding in JSON output.
    /// </summary>
    /// <param name="SmellId">The smell/rule id (e.g. DOC200).</param>
    /// <param name="Severity">The smell severity (Error/Warning/etc.).</param>
    /// <param name="FilePath">The absolute or relative file path.</param>
    /// <param name="TagName">The XML tag name associated with the finding.</param>
    /// <param name="Line">The 1-based line number.</param>
    /// <param name="Column">The 1-based column number.</param>
    /// <param name="Message">The formatted message.</param>
    /// <param name="Snippet">A short snippet for context.</param>
    internal sealed record JsonFindingDto(
        string SmellId,
        string Severity,
        string FilePath,
        string TagName,
        int Line,
        int Column,
        string Message,
        string Snippet)
    {
        /// <summary>
        /// Creates a <see cref="JsonFindingDto"/> from a domain <see cref="Finding"/>.
        /// </summary>
        /// <param name="finding">The finding to convert.</param>
        /// <returns>The converted DTO.</returns>
        public static JsonFindingDto FromFinding(Finding finding)
        {
            return new JsonFindingDto(
                SmellId: finding.Smell.Id,
                Severity: finding.Smell.Severity.ToString(),
                FilePath: finding.FilePath,
                TagName: finding.TagName,
                Line: finding.Line,
                Column: finding.Column,
                Message: finding.Message,
                Snippet: finding.Snippet);
        }
    }
}
