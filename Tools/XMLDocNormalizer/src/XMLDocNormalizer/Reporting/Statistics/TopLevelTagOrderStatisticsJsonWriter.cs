using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Writes top-level tag order statistics as JSON.
    /// </summary>
    internal static class TopLevelTagOrderStatisticsJsonWriter
    {
        /// <summary>
        /// Writes the statistics JSON report to the specified path.
        /// </summary>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="statistics">The statistics to serialize.</param>
        public static void Write(string outputPath, TopLevelTagOrderProjectStatistics statistics)
        {
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(statistics);

            string? directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(CreateSerializableObject(statistics), options);
            File.WriteAllText(outputPath, json);
        }

        /// <summary>
        /// Creates a stable, sorted object graph that is suitable for JSON serialization.
        /// </summary>
        /// <param name="statistics">The source statistics.</param>
        /// <returns>A serializable object with sorted data.</returns>
        private static object CreateSerializableObject(TopLevelTagOrderProjectStatistics statistics)
        {
            Dictionary<string, int> sortedRawSequences =
                SortFrequencyDictionary(statistics.RawSequenceFrequencies);

            Dictionary<string, int> sortedCollapsedSequences =
                SortFrequencyDictionary(statistics.CollapsedSequenceFrequencies);

            Dictionary<string, int> sortedTagPositions =
                SortTagPositionDictionary(statistics.TagPositionFrequencies);

            Dictionary<string, TopLevelTagPairwiseStatistic> sortedPairwise =
                statistics.PairwiseOrderingStatistics
                    .OrderBy(static pair => pair.Key)
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value);

            return new
            {
                statistics.ProjectName,
                statistics.TotalObservations,
                PairwiseOrderingStatistics = sortedPairwise,
                TagPositionFrequencies = sortedTagPositions,
                CollapsedSequenceFrequencies = sortedCollapsedSequences,
                RawSequenceFrequencies = sortedRawSequences,
            };
        }

        /// <summary>
        /// Sorts a frequency dictionary by descending count and then by key.
        /// </summary>
        /// <param name="source">The source dictionary.</param>
        /// <returns>A new sorted dictionary.</returns>
        private static Dictionary<string, int> SortFrequencyDictionary(
            Dictionary<string, int> source)
        {
            return source
                .OrderByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value);
        }

        /// <summary>
        /// Sorts a tag-position frequency dictionary by position and frequency.
        /// </summary>
        /// <param name="source">
        /// The source dictionary containing keys in the form <c>tag@position|context</c>.
        /// </param>
        /// <returns>
        /// A new dictionary ordered first by the extracted position (ascending),
        /// then by frequency (descending), and finally by key (ascending).
        /// </returns>
        /// <remarks>
        /// This ordering enables direct analysis of which tags most frequently occur
        /// at a given position within XML documentation comments.
        /// </remarks>
        private static Dictionary<string, int> SortTagPositionDictionary(
            Dictionary<string, int> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source
                .OrderBy(static pair => ExtractPosition(pair.Key))
                .ThenByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value);
        }

        /// <summary>
        /// Extracts the positional index from a tag-position key.
        /// </summary>
        /// <param name="key">
        /// The key in the format <c>tag@position|context</c>.
        /// </param>
        /// <returns>
        /// The parsed position if available; otherwise <see cref="int.MaxValue"/>.
        /// </returns>
        /// <remarks>
        /// The method expects a key containing an '@' followed by a numeric position.
        /// If the format is invalid or parsing fails, the maximum integer value is returned
        /// so that the entry is sorted to the end.
        /// </remarks>
        private static int ExtractPosition(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            int atIndex = key.IndexOf('@');
            if (atIndex < 0)
            {
                return int.MaxValue;
            }

            int pipeIndex = key.IndexOf('|', atIndex);

            string numberPart;

            if (pipeIndex < 0)
            {
                numberPart = key.Substring(atIndex + 1);
            }
            else
            {
                numberPart = key.Substring(atIndex + 1, pipeIndex - atIndex - 1);
            }

            if (int.TryParse(numberPart, out int position))
            {
                return position;
            }

            return int.MaxValue;
        }
    }
}
