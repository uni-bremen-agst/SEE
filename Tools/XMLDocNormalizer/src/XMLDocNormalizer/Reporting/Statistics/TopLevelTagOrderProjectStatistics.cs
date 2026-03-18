namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Represents aggregated top-level XML documentation tag order statistics for one project.
    /// </summary>
    internal sealed class TopLevelTagOrderProjectStatistics
    {
        /// <summary>
        /// Gets the relevant top-level tags used for statistics collection.
        /// </summary>
        public static IReadOnlyList<string> RelevantTags { get; } =
        [
            "summary",
            "typeparam",
            "param",
            "returns",
            "value",
            "exception",
            "remarks",
            "seealso"
        ];

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the total number of observed documentation comments.
        /// </summary>
        public int TotalObservations { get; private set; }

        /// <summary>
        /// Gets the frequencies of exact raw top-level tag sequences.
        /// </summary>
        public Dictionary<string, int> RawSequenceFrequencies { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the frequencies of normalized top-level tag sequences.
        /// </summary>
        public Dictionary<string, int> CollapsedSequenceFrequencies { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the frequencies of fully contextualized tag positions.
        /// Example key:
        /// <c>returns@3|has-summary:yes|has-typeparam:no|has-param:yes|has-value:no|has-exception:no|has-remarks:no|has-seealso:no</c>.
        /// </summary>
        public Dictionary<string, int> TagPositionFrequencies { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the pairwise ordering statistics for all relevant tag pairs.
        /// Example key: <c>typeparam|param</c>.
        /// </summary>
        public Dictionary<string, TopLevelTagPairwiseStatistic> PairwiseOrderingStatistics { get; } =
            new Dictionary<string, TopLevelTagPairwiseStatistic>(StringComparer.Ordinal);

        /// <summary>
        /// Adds one observation to the aggregated statistics.
        /// </summary>
        /// <param name="observation">The observation to add.</param>
        public void AddObservation(TopLevelTagOrderObservation observation)
        {
            ArgumentNullException.ThrowIfNull(observation);

            TotalObservations++;

            Increment(RawSequenceFrequencies, observation.RawSequenceKey);
            Increment(CollapsedSequenceFrequencies, observation.CollapsedSequenceKey);

            RecordTagPositions(observation);
            RecordPairwiseRelations(observation);
        }

        /// <summary>
        /// Merges another project statistics object into this one.
        /// </summary>
        /// <param name="other">The statistics to merge in.</param>
        public void Merge(TopLevelTagOrderProjectStatistics other)
        {
            ArgumentNullException.ThrowIfNull(other);

            TotalObservations += other.TotalObservations;

            MergeFrequencyDictionary(RawSequenceFrequencies, other.RawSequenceFrequencies);
            MergeFrequencyDictionary(CollapsedSequenceFrequencies, other.CollapsedSequenceFrequencies);
            MergeFrequencyDictionary(TagPositionFrequencies, other.TagPositionFrequencies);
            MergePairwiseDictionary(PairwiseOrderingStatistics, other.PairwiseOrderingStatistics);
        }

        /// <summary>
        /// Gets percentage frequencies for normalized sequences.
        /// </summary>
        /// <returns>A dictionary of normalized-sequence percentages.</returns>
        public Dictionary<string, double> GetCollapsedSequencePercentages()
        {
            return ToPercentages(CollapsedSequenceFrequencies, TotalObservations);
        }

        /// <summary>
        /// Gets percentage frequencies for tag positions.
        /// </summary>
        /// <returns>A dictionary of tag-position percentages.</returns>
        public Dictionary<string, double> GetTagPositionPercentages()
        {
            return ToPercentages(TagPositionFrequencies, TotalObservations);
        }

        /// <summary>
        /// Records fully contextualized tag positions based on the normalized sequence.
        /// </summary>
        /// <param name="observation">The observation to inspect.</param>
        private void RecordTagPositions(TopLevelTagOrderObservation observation)
        {
            IReadOnlyList<string> sequence = observation.CollapsedSequence;

            for (int i = 0; i < sequence.Count; i++)
            {
                string key = BuildPositionKey(sequence[i], i + 1, observation);
                Increment(TagPositionFrequencies, key);
            }
        }

        /// <summary>
        /// Records pairwise ordering relations for all relevant tag pairs.
        /// </summary>
        /// <param name="observation">The observation to inspect.</param>
        private void RecordPairwiseRelations(TopLevelTagOrderObservation observation)
        {
            IReadOnlyList<string> sequence = observation.CollapsedSequence;

            for (int i = 0; i < RelevantTags.Count; i++)
            {
                for (int j = i + 1; j < RelevantTags.Count; j++)
                {
                    RecordPairwiseRelation(sequence, RelevantTags[i], RelevantTags[j]);
                }
            }
        }

        /// <summary>
        /// Builds a fully contextualized tag-position key.
        /// </summary>
        /// <param name="tagName">The tag name.</param>
        /// <param name="position">The one-based position in the normalized sequence.</param>
        /// <param name="observation">The observation context.</param>
        /// <returns>A normalized tag-position key.</returns>
        private static string BuildPositionKey(
            string tagName,
            int position,
            TopLevelTagOrderObservation observation)
        {
            return tagName + "@"
                + position
                + "|has-summary:" + BoolToYesNo(observation.HasTag("summary"))
                + "|has-typeparam:" + BoolToYesNo(observation.HasTag("typeparam"))
                + "|has-param:" + BoolToYesNo(observation.HasTag("param"))
                + "|has-returns:" + BoolToYesNo(observation.HasTag("returns"))
                + "|has-value:" + BoolToYesNo(observation.HasTag("value"))
                + "|has-exception:" + BoolToYesNo(observation.HasTag("exception"))
                + "|has-remarks:" + BoolToYesNo(observation.HasTag("remarks"))
                + "|has-seealso:" + BoolToYesNo(observation.HasTag("seealso"));
        }

        /// <summary>
        /// Records whether one tag appears before another tag.
        /// </summary>
        /// <param name="sequence">The normalized tag sequence.</param>
        /// <param name="leftTag">The left tag.</param>
        /// <param name="rightTag">The right tag.</param>
        private void RecordPairwiseRelation(
            IReadOnlyList<string> sequence,
            string leftTag,
            string rightTag)
        {
            int leftIndex = IndexOf(sequence, leftTag);
            int rightIndex = IndexOf(sequence, rightTag);

            if ((leftIndex < 0) || (rightIndex < 0))
            {
                return;
            }

            string key = leftTag + "|" + rightTag;

            if (!PairwiseOrderingStatistics.TryGetValue(key, out TopLevelTagPairwiseStatistic? statistic))
            {
                statistic = new TopLevelTagPairwiseStatistic
                {
                    LeftTag = leftTag,
                    RightTag = rightTag
                };

                PairwiseOrderingStatistics.Add(key, statistic);
            }

            statistic.TotalComparableObservations++;

            if (leftIndex < rightIndex)
            {
                statistic.LeftBeforeRightCount++;
                return;
            }

            statistic.RightBeforeLeftCount++;
        }

        /// <summary>
        /// Converts a Boolean value to a stable yes/no text representation.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><c>yes</c> or <c>no</c>.</returns>
        private static string BoolToYesNo(bool value)
        {
            return value ? "yes" : "no";
        }

        /// <summary>
        /// Gets the index of a tag in a sequence.
        /// </summary>
        /// <param name="sequence">The sequence to inspect.</param>
        /// <param name="tagName">The tag name to search for.</param>
        /// <returns>The zero-based index if found; otherwise <c>-1</c>.</returns>
        private static int IndexOf(IReadOnlyList<string> sequence, string tagName)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                if (sequence[i] == tagName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Increments a frequency dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to update.</param>
        /// <param name="key">The key to increment.</param>
        private static void Increment(Dictionary<string, int> dictionary, string key)
        {
            if (dictionary.TryGetValue(key, out int count))
            {
                dictionary[key] = count + 1;
                return;
            }

            dictionary.Add(key, 1);
        }

        /// <summary>
        /// Merges simple frequency dictionaries.
        /// </summary>
        /// <param name="target">The target dictionary.</param>
        /// <param name="source">The source dictionary.</param>
        private static void MergeFrequencyDictionary(
            Dictionary<string, int> target,
            Dictionary<string, int> source)
        {
            foreach (KeyValuePair<string, int> pair in source)
            {
                if (target.TryGetValue(pair.Key, out int count))
                {
                    target[pair.Key] = count + pair.Value;
                    continue;
                }

                target.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Merges pairwise-statistics dictionaries.
        /// </summary>
        /// <param name="target">The target dictionary.</param>
        /// <param name="source">The source dictionary.</param>
        private static void MergePairwiseDictionary(
            Dictionary<string, TopLevelTagPairwiseStatistic> target,
            Dictionary<string, TopLevelTagPairwiseStatistic> source)
        {
            foreach (KeyValuePair<string, TopLevelTagPairwiseStatistic> pair in source)
            {
                if (target.TryGetValue(pair.Key, out TopLevelTagPairwiseStatistic? statistic))
                {
                    statistic.Merge(pair.Value);
                    continue;
                }

                TopLevelTagPairwiseStatistic clone = new TopLevelTagPairwiseStatistic
                {
                    LeftTag = pair.Value.LeftTag,
                    RightTag = pair.Value.RightTag,
                    TotalComparableObservations = pair.Value.TotalComparableObservations,
                    LeftBeforeRightCount = pair.Value.LeftBeforeRightCount,
                    RightBeforeLeftCount = pair.Value.RightBeforeLeftCount
                };

                target.Add(pair.Key, clone);
            }
        }

        /// <summary>
        /// Converts frequencies to percentages.
        /// </summary>
        /// <param name="frequencies">The frequency dictionary.</param>
        /// <param name="total">The total observation count.</param>
        /// <returns>A percentage dictionary.</returns>
        private static Dictionary<string, double> ToPercentages(
            Dictionary<string, int> frequencies,
            int total)
        {
            Dictionary<string, double> result =
                new Dictionary<string, double>(StringComparer.Ordinal);

            if (total == 0)
            {
                return result;
            }

            foreach (KeyValuePair<string, int> pair in frequencies)
            {
                result.Add(pair.Key, pair.Value * 100.0 / total);
            }

            return result;
        }
    }
}
