using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Represents a single finding in JSON output.
    /// </summary>
    /// <param name="SmellId">The smell rule identifier.</param>
    /// <param name="Severity">The smell severity.</param>
    /// <param name="FilePath">The absolute or relative source file path.</param>
    /// <param name="TagName">The XML documentation tag name associated with the finding.</param>
    /// <param name="OwnerKind">The kind of source declaration that owns the documentation comment.</param>
    /// <param name="SubjectKind">The concrete documentation subject affected by the finding.</param>
    /// <param name="Accessibility">The declared or inferred accessibility of the owner declaration.</param>
    /// <param name="SymbolName">The source symbol name of the owner declaration.</param>
    /// <param name="ContainingType">The containing type name, if one exists.</param>
    /// <param name="ContainingNamespace">The containing namespace name.</param>
    /// <param name="TargetName">The concrete affected target name, if one exists.</param>
    /// <param name="ProjectName">The analyzed project name, if available.</param>
    /// <param name="IsGenerated">Indicates whether the source file appears to be generated code, if known.</param>
    /// <param name="IsTestFile">Indicates whether the source file appears to be test code, if known.</param>
    /// <param name="Line">The one-based line number.</param>
    /// <param name="Column">The one-based column number.</param>
    /// <param name="Message">The formatted finding message.</param>
    /// <param name="Snippet">A short source snippet for context.</param>
    internal sealed record JsonFindingDto(
        string SmellId,
        string Severity,
        string FilePath,
        string TagName,
        string OwnerKind,
        string SubjectKind,
        string Accessibility,
        string SymbolName,
        string ContainingType,
        string ContainingNamespace,
        string? TargetName,
        string? ProjectName,
        bool? IsGenerated,
        bool? IsTestFile,
        int Line,
        int Column,
        string Message,
        string Snippet)
    {
        /// <summary>
        /// Creates a JSON finding DTO from a domain finding.
        /// </summary>
        /// <param name="finding">The finding to convert.</param>
        /// <returns>
        /// The converted JSON finding DTO.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when finding is null.</exception>
        public static JsonFindingDto FromFinding(Finding finding)
        {
            ArgumentNullException.ThrowIfNull(finding);

            FindingContext context = finding.Context;

            return new JsonFindingDto(
                SmellId: finding.Smell.ID,
                Severity: finding.Smell.Severity.ToString(),
                FilePath: finding.FilePath,
                TagName: finding.TagName,
                OwnerKind: context.OwnerKind,
                SubjectKind: context.SubjectKind,
                Accessibility: context.Accessibility,
                SymbolName: context.SymbolName,
                ContainingType: context.ContainingType,
                ContainingNamespace: context.ContainingNamespace,
                TargetName: context.TargetName,
                ProjectName: context.ProjectName,
                IsGenerated: context.IsGenerated,
                IsTestFile: context.IsTestFile,
                Line: finding.Line,
                Column: finding.Column,
                Message: finding.Message,
                Snippet: finding.Snippet);
        }
    }
}
