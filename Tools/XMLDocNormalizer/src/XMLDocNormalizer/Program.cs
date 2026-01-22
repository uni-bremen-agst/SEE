using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer
{
    /// <summary>
    /// Entry point for the XMLDocNormalizer command-line tool.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Process exit code.</returns>
        static int Main(string[] args)
        {
            if (!ArgParsing.TryParseOptions(args, out ToolOptions? options) || options == null)
            {
                return ToolExitCodes.InvalidArguments;
            }

            if (options.CleanBackups)
            {
                BackupManager.DeleteOldBackups(options.TargetPath);
            }

            RunResult result = ToolRunner.Run(options);

            EvaluateResult(options, result);

            return result.FindingCount > 0
                ? ToolExitCodes.Findings
                : ToolExitCodes.Success;
        }

        /// <summary>
        /// Prints a human-readable summary of the run.
        /// </summary>
        /// <param name="options">The parsed tool options.</param>
        /// <param name="result">The aggregated run result.</param>
        private static void EvaluateResult(ToolOptions options, RunResult result)
        {
            if (options.CheckOnly)
            {
                if (result.FindingCount == 0)
                {
                    Console.WriteLine("Check passed: no documentation issues found.");
                }
                else
                {
                    Console.WriteLine($"Check failed: {result.FindingCount} documentation issue(s) found.");
                }
            }
            else
            {
                Console.WriteLine($"Done. Changed files: {result.ChangedFiles}. Findings: {result.FindingCount}.");
            }
        }
    }
}
