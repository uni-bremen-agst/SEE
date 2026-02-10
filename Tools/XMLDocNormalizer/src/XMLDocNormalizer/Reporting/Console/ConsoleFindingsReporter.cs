using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;

namespace XMLDocNormalizer.Reporting.Console
{
    /// <summary>
    /// Writes findings to standard output in a human-readable format.
    /// </summary>
    /// <remarks>
    /// This reporter is intended for interactive local usage and plain logs.
    /// It does not buffer output and has no side effects in <see cref="Complete"/>.
    /// </remarks>
    internal sealed class ConsoleFindingsReporter : IFindingsReporter
    {
        /// <summary>
        /// Reports findings for a single file by writing them to the console.
        /// </summary>
        /// <param name="filePath">The analyzed file path.</param>
        /// <param name="findings">The findings produced for the file.</param>
        public void ReportFile(string filePath, IReadOnlyList<Finding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                return;
            }

            System.Console.WriteLine();
            System.Console.WriteLine(filePath);

            foreach (Finding finding in findings)
            {
                System.Console.WriteLine(FormatFindingLine(finding));
            }
        }

        /// <summary>
        /// Finalizes reporting. No action is required for console output.
        /// </summary>
        public void Complete()
        {
            // No buffering for console output.
        }

        /// <summary>
        /// Formats a finding as a single console line.
        /// </summary>
        /// <param name="finding">The finding to format.</param>
        /// <returns>A formatted single-line representation.</returns>
        private static string FormatFindingLine(Finding finding)
        {
            // Example:
            // [DOC200|Error] [12,5] <summary>: <summary> is missing. | snippet...
            return $"[{finding.Smell.Id}|{finding.Smell.Severity}] [{finding.Line},{finding.Column}] <{finding.TagName}>: {finding.Message} | {finding.Snippet}";
        }
    }
}
