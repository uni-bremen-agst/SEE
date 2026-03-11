using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Models.Keys;

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
                ChangedFiles = result.ChangedFiles,
                Totals = CopyTotals(result.Totals),
                TotalFindingCounts = CopyTotalsOrderedByKey(result.SmellCounts),
                Coverage = CalculateCoverage(result)
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

        /// <summary>
        /// Creates a defensive copy of run totals.
        /// </summary>
        /// <param name="totals">The run totals dictionary.</param>
        /// <returns>A new dictionary containing the same totals.</returns>
        private static Dictionary<string, int> CopyTotals(Dictionary<string, int> totals)
        {
            Dictionary<string, int> copy = new(StringComparer.Ordinal);

            if (totals == null || totals.Count == 0)
            {
                return copy;
            }

            foreach (KeyValuePair<string, int> pair in totals)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }

        /// <summary>
        /// Creates a copy of the given <paramref name="totals"/> dictionary
        /// and returns the entries sorted by key in ascending order.
        /// </summary>
        /// <param name="totals">The original dictionary whose entries should be copied and sorted.</param>
        /// <returns>
        /// A new <see cref="Dictionary{TKey, TValue}"/> containing the same key-value pairs
        /// as <paramref name="totals"/>, but sorted by key in ascending order.
        /// </returns>
        /// <remarks>
        /// This method uses the helper method <see cref="CopyTotals"/> to create a shallow copy of the dictionary.
        /// The entries are then sorted by their keys and converted back into a new dictionary.
        /// </remarks>
        private static Dictionary<string, int> CopyTotalsOrderedByKey(Dictionary<string, int> totals)
        {
            return CopyTotals(totals)
                    .OrderBy(pair => pair.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        /// <summary>
        /// Calculates derived coverage ratios from smell counts and totals.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <returns>A dictionary of coverage ratios keyed by <see cref="CoverageKeys"/>.</returns>
        private static Dictionary<string, double> CalculateCoverage(RunResult result)
        {
            Dictionary<string, double> coverage = new(StringComparer.Ordinal);

            int parametersTotal = GetTotal(result, StatisticsKeys.ParametersTotal);
            if (parametersTotal > 0)
            {
                int missingParamTags = GetSmellCount(result, "DOC310");
                int emptyParamDescriptions = GetSmellCount(result, "DOC320");

                coverage[CoverageKeys.ParamMissingTagRate] = missingParamTags / (double)parametersTotal;
                coverage[CoverageKeys.ParamEmptyDescriptionRate] = emptyParamDescriptions / (double)parametersTotal;
            }

            int typeParametersTotal = GetTotal(result, StatisticsKeys.TypeParametersTotal);
            if (typeParametersTotal > 0)
            {
                int missingTypeParamTags = GetSmellCount(result, "DOC410");
                int emptyTypeParamDescriptions = GetSmellCount(result, "DOC420");

                coverage[CoverageKeys.TypeParamMissingTagRate] = missingTypeParamTags / (double)typeParametersTotal;
                coverage[CoverageKeys.TypeParamEmptyDescriptionRate] = emptyTypeParamDescriptions / (double)typeParametersTotal;
            }

            int returnsRequiredTotal = GetTotal(result, StatisticsKeys.ReturnsRequiredTotal);
            if (returnsRequiredTotal > 0)
            {
                int missingReturns = GetSmellCount(result, "DOC500");
                coverage[CoverageKeys.ReturnsMissingRate] = missingReturns / (double)returnsRequiredTotal;
            }

            int uniqueNamespacesTotal = GetTotal(result, StatisticsKeys.UniqueNamespacesTotal);
            if (uniqueNamespacesTotal > 0)
            {
                int missingCentralNamespaceDoc = GetSmellCount(result, "DOC101");
                coverage[CoverageKeys.NamespaceCentralDocMissingRate] =
                    missingCentralNamespaceDoc / (double)uniqueNamespacesTotal;
            }

            return coverage;
        }

        /// <summary>
        /// Reads a total value from the run totals dictionary.
        /// </summary>
        /// <param name="result">The run result.</param>
        /// <param name="key">Totals key.</param>
        /// <returns>The stored value or 0 if missing.</returns>
        private static int GetTotal(RunResult result, string key)
        {
            if (!result.Totals.TryGetValue(key, out int value))
            {
                return 0;
            }

            return value;
        }

        /// <summary>
        /// Reads a smell count value from the run smell counts dictionary.
        /// </summary>
        /// <param name="result">The run result.</param>
        /// <param name="smellId">Smell id (e.g. DOC500).</param>
        /// <returns>The stored value or 0 if missing.</returns>
        private static int GetSmellCount(RunResult result, string smellId)
        {
            if (!result.SmellCounts.TryGetValue(smellId, out int value))
            {
                return 0;
            }

            return value;
        }
    }
}