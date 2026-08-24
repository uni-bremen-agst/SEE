using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;

namespace XMLDocNormalizer.Reporting.Console
{
    /// <summary>
    /// Writes findings to standard output in a human-readable format.
    /// </summary>
    /// <remarks>
    /// This reporter is intended for interactive local usage and plain logs.
    /// It does not buffer output and has no side effects in Complete.
    /// In verbose mode, each finding is followed by an additional context line.
    /// </remarks>
    internal sealed class ConsoleFindingsReporter : IFindingsReporter
    {
        /// <summary>
        /// Stores whether verbose finding context output is enabled.
        /// </summary>
        private readonly bool verbose;

        /// <summary>
        /// Initializes a new instance of the ConsoleFindingsReporter class with verbose output disabled.
        /// </summary>
        public ConsoleFindingsReporter()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConsoleFindingsReporter class.
        /// </summary>
        /// <param name="verbose">
        /// Indicates whether study-oriented finding context metadata should be printed below each finding.
        /// </param>
        public ConsoleFindingsReporter(bool verbose)
        {
            this.verbose = verbose;
        }

        /// <summary>
        /// Reports findings for a single file by writing them to the console.
        /// </summary>
        /// <param name="filePath">The analyzed file path.</param>
        /// <param name="findings">The findings produced for the file.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when verbose context formatting is enabled and
        /// <paramref name="findings"/> contains a null element.
        /// </exception>
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
                System.Console.WriteLine(finding);

                if (verbose)
                {
                    System.Console.WriteLine("  " + ConsoleFindingContextFormatter.Format(finding));
                }
            }
        }

        /// <summary>
        /// Finalizes reporting. No action is required for console output.
        /// </summary>
        public void Complete()
        {
            // No buffering for console output.
        }
    }
}
