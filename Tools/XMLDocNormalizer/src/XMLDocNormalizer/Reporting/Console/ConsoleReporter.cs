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
        public static void PrintFindings(string filePath, List<Finding> findings)
        {
            System.Console.WriteLine($"Findings in {filePath}:");
            foreach (Finding finding in findings)
            {
                System.Console.WriteLine("  " + finding.ToString());
            }
        }
    }
}
