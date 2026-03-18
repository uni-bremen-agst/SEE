using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Writes human-readable top-level tag order statistics reports.
    /// </summary>
    internal static class TopLevelTagOrderStatisticsTextWriter
    {
        /// <summary>
        /// Creates a human-readable report for the collected statistics.
        /// </summary>
        /// <param name="statistics">The statistics to format.</param>
        /// <returns>The formatted text report.</returns>
        public static string WriteToString(TopLevelTagOrderProjectStatistics statistics)
        {
            ArgumentNullException.ThrowIfNull(statistics);

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Top-level XML documentation tag order statistics");
            builder.AppendLine("================================================");
            builder.AppendLine("Project: " + statistics.ProjectName);
            builder.AppendLine("Total observations: " + statistics.TotalObservations);
            builder.AppendLine();

            AppendCollapsedSequences(builder, statistics);
            AppendTagPositions(builder, statistics);
            AppendPairwiseOrdering(builder, statistics);
            AppendEmpiricalOrder(builder, statistics);

            return builder.ToString();
        }

        /// <summary>
        /// Writes the report to a file.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="statistics">The statistics to format.</param>
        public static void Write(string outputPath, TopLevelTagOrderProjectStatistics statistics)
        {
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(statistics);

            string? directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string report = WriteToString(statistics);
            File.WriteAllText(outputPath, report);
        }

        /// <summary>
        /// Appends the most frequent collapsed sequences.
        /// </summary>
        /// <param name="builder">The output builder.</param>
        /// <param name="statistics">The statistics to format.</param>
        private static void AppendCollapsedSequences(
            StringBuilder builder,
            TopLevelTagOrderProjectStatistics statistics)
        {
            builder.AppendLine("Most frequent normalized sequences");
            builder.AppendLine("----------------------------------");

            Dictionary<string, double> percentages = statistics.GetCollapsedSequencePercentages();

            foreach (KeyValuePair<string, int> pair in statistics.CollapsedSequenceFrequencies
                         .OrderByDescending(static pair => pair.Value)
                         .ThenBy(static pair => pair.Key)
                         .Take(20))
            {
                double percentage = percentages[pair.Key];
                builder.AppendLine(
                    "  "
                    + pair.Value
                    + "x  ("
                    + percentage.ToString("0.00")
                    + "%)  "
                    + pair.Key);
            }

            builder.AppendLine();
        }

        /// <summary>
        /// Appends the most frequent context-aware tag positions.
        /// </summary>
        /// <param name="builder">The output builder.</param>
        /// <param name="statistics">The statistics to format.</param>
        private static void AppendTagPositions(
            StringBuilder builder,
            TopLevelTagOrderProjectStatistics statistics)
        {
            builder.AppendLine("Most frequent context-aware tag positions");
            builder.AppendLine("-----------------------------------------");

            Dictionary<string, double> percentages = statistics.GetTagPositionPercentages();

            foreach (KeyValuePair<string, int> pair in statistics.TagPositionFrequencies
                         .OrderByDescending(static pair => pair.Value)
                         .ThenBy(static pair => pair.Key)
                         .Take(25))
            {
                double percentage = percentages[pair.Key];
                builder.AppendLine(
                    "  "
                    + pair.Value
                    + "x  ("
                    + percentage.ToString("0.00")
                    + "%)  "
                    + pair.Key);
            }

            builder.AppendLine();
        }

        /// <summary>
        /// Appends pairwise ordering statistics.
        /// </summary>
        /// <param name="builder">The output builder.</param>
        /// <param name="statistics">The statistics to format.</param>
        private static void AppendPairwiseOrdering(
            StringBuilder builder,
            TopLevelTagOrderProjectStatistics statistics)
        {
            builder.AppendLine("Pairwise ordering");
            builder.AppendLine("-----------------");

            foreach (KeyValuePair<string, TopLevelTagPairwiseStatistic> pair in statistics.PairwiseOrderingStatistics
                         .OrderBy(static pair => pair.Key))
            {
                TopLevelTagPairwiseStatistic value = pair.Value;

                builder.AppendLine("  " + value.LeftTag + " before " + value.RightTag);
                builder.AppendLine("    Comparable observations: " + value.TotalComparableObservations);
                builder.AppendLine("    Left before right:       " + value.LeftBeforeRightCount
                    + " (" + value.LeftBeforeRightPercentage.ToString("0.00") + "%)");
                builder.AppendLine("    Right before left:       " + value.RightBeforeLeftCount
                    + " (" + value.RightBeforeLeftPercentage.ToString("0.00") + "%)");
            }

            builder.AppendLine();
        }

        /// <summary>
        /// Appends the empirically resolved top-level tag order.
        /// </summary>
        /// <param name="builder">The output builder.</param>
        /// <param name="statistics">The statistics to format.</param>
        private static void AppendEmpiricalOrder(
            StringBuilder builder,
            TopLevelTagOrderProjectStatistics statistics)
        {
            TopLevelTagOrderResolution resolution =
                TopLevelTagOrderResolver.Resolve(statistics);

            builder.AppendLine("Empirical order recommendation");
            builder.AppendLine("------------------------------");
            builder.AppendLine("  " + resolution.ReadableOrder);
            builder.AppendLine();

            builder.AppendLine("Accepted relations:");
            foreach (string relation in resolution.AcceptedRelations
                         .OrderBy(static relation => relation, StringComparer.Ordinal))
            {
                builder.AppendLine("  " + relation);
            }

            builder.AppendLine();
            builder.AppendLine("Ignored relations:");
            foreach (string relation in resolution.IgnoredRelations
                         .OrderBy(static relation => relation, StringComparer.Ordinal))
            {
                builder.AppendLine("  " + relation);
            }

            builder.AppendLine();
        }
    }
}
