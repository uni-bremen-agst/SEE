using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Calculates run metrics derived from a <see cref="RunResult"/>.
    /// </summary>
    internal static class RunMetricsCalculator
    {
        /// <summary>
        /// Creates a <see cref="RunMetricsDto"/> snapshot from an aggregated <see cref="RunResult"/>.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <returns>The calculated metrics snapshot.</returns>
        public static RunMetricsDto From(RunResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            RunMetricsDto metrics = new()
            {
                Sloc = result.Sloc,
                FindingCount = result.FindingCount,
                ErrorCount = result.ErrorCount,
                WarningCount = result.WarningCount,
                SuggestionCount = result.SuggestionCount,
                ChangedFiles = result.ChangedFiles
            };

            if (result.Sloc > 0)
            {
                metrics.FindingsPerKLoc = PerKLoc(result.FindingCount, result.Sloc);
                metrics.ErrorsPerKLoc = PerKLoc(result.ErrorCount, result.Sloc);
                metrics.WarningsPerKLoc = PerKLoc(result.WarningCount, result.Sloc);
                metrics.SuggestionsPerKLoc = PerKLoc(result.SuggestionCount, result.Sloc);
            }

            return metrics;
        }

        /// <summary>
        /// Calculates a density per 1000 SLOC.
        /// </summary>
        /// <param name="count">The absolute count.</param>
        /// <param name="sloc">The total SLOC.</param>
        /// <returns>The density per 1000 SLOC.</returns>
        private static double PerKLoc(int count, int sloc)
        {
            if (sloc <= 0)
            {
                return 0.0;
            }

            return count / (sloc / 1000.0);
        }
    }
}