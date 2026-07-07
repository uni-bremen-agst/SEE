using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Console
{
    /// <summary>
    /// Provides console output helpers for reporting findings.
    /// </summary>
    internal static class ConsoleReporter
    {
        /// <summary>
        /// Prints findings for a file to the console.
        /// </summary>
        /// <param name="filePath">The file path being reported.</param>
        /// <param name="findings">The findings to print.</param>
        /// <param name="verbose">
        /// Indicates whether study-oriented finding context metadata should be printed below each finding.
        /// </param>
        public static void PrintFindings(
            string filePath,
            List<Finding> findings,
            bool verbose = false)
        {
            System.Console.WriteLine($"Findings in {filePath}:");

            foreach (Finding finding in findings)
            {
                System.Console.WriteLine("  " + finding);

                if (verbose)
                {
                    System.Console.WriteLine("    " + ConsoleFindingContextFormatter.Format(finding));
                }
            }
        }
    }
}
