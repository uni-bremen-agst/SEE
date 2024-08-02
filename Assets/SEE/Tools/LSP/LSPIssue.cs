using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.UI.Window.CodeWindow;
using Range = SEE.DataModel.DG.Range;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// A code issue diagnosed by a language server.
    /// </summary>
    /// <param name="Path">The path of the file where the issue was diagnosed.</param>
    /// <param name="Diagnostic">The diagnostic that represents the issue.</param>
    public record LSPIssue(string Path, Diagnostic Diagnostic) : IDisplayableIssue
    {
        /// <summary>
        /// Implements <see cref="IDisplayableIssue.ToDisplayStringAsync"/>.
        /// </summary>
        public UniTask<string> ToDisplayStringAsync()
        {
            string message = "";
            if (Diagnostic.Code.HasValue)
            {
                message += $"<b>{Diagnostic.Code.Value.String ?? Diagnostic.Code.Value.Long.ToString()}</b>: ";
            }
            message += $"<noparse>{Diagnostic.Message}</noparse>";
            return UniTask.FromResult(message);
        }

        /// <summary>
        /// Implements <see cref="IDisplayableIssue.Source"/>.
        /// </summary>
        public string Source => Diagnostic.Source ?? "LSP";

        /// <summary>
        /// Implements <see cref="IDisplayableIssue.RichTags"/>.
        /// </summary>
        public IList<string> RichTags
        {
            get
            {
                List<DiagnosticTag> tags = Diagnostic.Tags?.ToList() ?? new();
                if (tags.Count > 0)
                {
                    return tags.Select(DiagnosticTagToRichTag).ToList();
                }
                else
                {
                    // If there are no explicit tags, we create a tag based on the severity.
                    return new List<string>
                    {
                        DiagnosticSeverityToTag(Diagnostic.Severity ?? DiagnosticSeverity.Warning)
                    };
                }
            }
        }

        /// <summary>
        /// Converts a diagnostic tag to a TextMeshPro rich text tag, intended to be used within code windows.
        /// </summary>
        /// <param name="tag">The diagnostic tag to convert.</param>
        /// <returns>The TextMeshPro rich text tag that corresponds to the given <paramref name="tag"/>.</returns>
        private static string DiagnosticTagToRichTag(DiagnosticTag tag) =>
            tag switch
            {
                DiagnosticTag.Unnecessary => "<color=#666B7D>",
                DiagnosticTag.Deprecated => "<s>",
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "Unknown diagnostic tag")
            };

        /// <summary>
        /// Converts a diagnostic severity to a TextMeshPro rich text tag, intended to be used within code windows.
        /// </summary>
        /// <param name="severity">The diagnostic severity to convert.</param>
        /// <returns>The TextMeshPro rich text tag that corresponds to the given <paramref name="severity"/>.</returns>
        private static string DiagnosticSeverityToTag(DiagnosticSeverity severity) =>
            severity switch
            {
                DiagnosticSeverity.Error => "<mark=#FF537033>",
                DiagnosticSeverity.Warning => "<mark=#FFCB6B33>",
                DiagnosticSeverity.Information => "<mark=#89DDFF33>",
                DiagnosticSeverity.Hint => "<mark=#89DDFF22>",
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown diagnostic severity")
            };

        /// <summary>
        /// Implements <see cref="IDisplayableIssue.Occurrences"/>.
        /// </summary>
        public IEnumerable<(string Path, Range Range)> Occurrences
        {
            get
            {
                List<(string Path, Range Range)> occurrences = new()
                {
                    (Path, Range.FromLspRange(Diagnostic.Range))
                };
                if (Diagnostic.RelatedInformation != null)
                {
                    occurrences.AddRange(Diagnostic.RelatedInformation.Select(x => (x.Location.Uri.GetFileSystemPath(), Range.FromLspRange(x.Location.Range))));
                }
                return occurrences;
            }
        }
    }
}
