namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Resolves an empirical top-level XML documentation tag order from pairwise statistics.
    /// </summary>
    internal static class TopLevelTagOrderResolver
    {
        /// <summary>
        /// Resolves an empirical tag order from the supplied project statistics.
        /// </summary>
        /// <param name="statistics">The project statistics.</param>
        /// <returns>The resolved empirical order.</returns>
        public static TopLevelTagOrderResolution Resolve(TopLevelTagOrderProjectStatistics statistics)
        {
            ArgumentNullException.ThrowIfNull(statistics);

            TopLevelTagOrderResolution resolution = new TopLevelTagOrderResolution();
            List<PairwiseRelationDecision> acceptedDecisions = new List<PairwiseRelationDecision>();
            HashSet<string> activeTags = new HashSet<string>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, TopLevelTagPairwiseStatistic> pair in statistics.PairwiseOrderingStatistics)
            {
                TopLevelTagPairwiseStatistic value = pair.Value;

                PairwiseRelationDecision decision = DecideRelation(value);

                if (!decision.IsAccepted)
                {
                    resolution.IgnoredRelations.Add(decision.Description);
                    continue;
                }

                acceptedDecisions.Add(decision);
                activeTags.Add(decision.BeforeTag);
                activeTags.Add(decision.AfterTag);
                resolution.AcceptedRelations.Add(decision.Description);
            }

            List<string> orderedActiveTags =
                activeTags
                    .OrderBy(static tag => tag, StringComparer.Ordinal)
                    .ToList();

            Dictionary<string, HashSet<string>> outgoingEdges =
                CreateEmptyAdjacencyMap(orderedActiveTags);

            Dictionary<string, HashSet<string>> incomingEdges =
                CreateEmptyAdjacencyMap(orderedActiveTags);

            foreach (PairwiseRelationDecision decision in acceptedDecisions)
            {
                outgoingEdges[decision.BeforeTag].Add(decision.AfterTag);
                incomingEdges[decision.AfterTag].Add(decision.BeforeTag);
            }

            List<IReadOnlyList<string>> orderedTiers =
                ResolveTieredTopologicalOrder(outgoingEdges, incomingEdges, orderedActiveTags);

            foreach (IReadOnlyList<string> tier in orderedTiers)
            {
                resolution.OrderedTiers.Add(tier);
            }

            List<string> inactiveTags =
                TopLevelTagOrderProjectStatistics.RelevantTags
                    .Where(tag => !activeTags.Contains(tag))
                    .OrderBy(static tag => tag, StringComparer.Ordinal)
                    .ToList();

            foreach (string inactiveTag in inactiveTags)
            {
                resolution.InactiveTags.Add(inactiveTag);
            }

            return resolution;
        }

        /// <summary>
        /// Decides whether a pairwise relation is strong enough to be accepted.
        /// </summary>
        /// <param name="statistic">The pairwise statistic to evaluate.</param>
        /// <returns>The decision result.</returns>
        private static PairwiseRelationDecision DecideRelation(TopLevelTagPairwiseStatistic statistic)
        {
            const int MinimumComparableObservations = 10;
            const double MinimumDominancePercentage = 60.0;
            const double MinimumDifferencePercentage = 20.0;

            if (statistic.TotalComparableObservations < MinimumComparableObservations)
            {
                return PairwiseRelationDecision.CreateIgnored(
                    statistic.LeftTag,
                    statistic.RightTag,
                    "ignored: too few comparable observations ("
                    + statistic.TotalComparableObservations
                    + ")");
            }

            double leftPercentage = statistic.LeftBeforeRightPercentage;
            double rightPercentage = statistic.RightBeforeLeftPercentage;
            double difference = Math.Abs(leftPercentage - rightPercentage);

            if ((leftPercentage < MinimumDominancePercentage)
                && (rightPercentage < MinimumDominancePercentage))
            {
                return PairwiseRelationDecision.CreateIgnored(
                    statistic.LeftTag,
                    statistic.RightTag,
                    "ignored: no dominant direction ("
                    + statistic.LeftTag
                    + " "
                    + leftPercentage.ToString("0.00")
                    + "% vs "
                    + statistic.RightTag
                    + " "
                    + rightPercentage.ToString("0.00")
                    + "%)");
            }

            if (difference < MinimumDifferencePercentage)
            {
                return PairwiseRelationDecision.CreateIgnored(
                    statistic.LeftTag,
                    statistic.RightTag,
                    "ignored: difference too small ("
                    + difference.ToString("0.00")
                    + "%)");
            }

            if (leftPercentage > rightPercentage)
            {
                return PairwiseRelationDecision.CreateAccepted(
                    statistic.LeftTag,
                    statistic.RightTag,
                    statistic.LeftTag
                    + " before "
                    + statistic.RightTag
                    + " ("
                    + leftPercentage.ToString("0.00")
                    + "%, n="
                    + statistic.TotalComparableObservations
                    + ")");
            }

            return PairwiseRelationDecision.CreateAccepted(
                statistic.RightTag,
                statistic.LeftTag,
                statistic.RightTag
                + " before "
                + statistic.LeftTag
                + " ("
                + rightPercentage.ToString("0.00")
                + "%, n="
                + statistic.TotalComparableObservations
                + ")");
        }

        /// <summary>
        /// Resolves a tiered topological order from the accepted pairwise relations.
        /// </summary>
        /// <param name="outgoingEdges">The outgoing adjacency map.</param>
        /// <param name="incomingEdges">The incoming adjacency map.</param>
        /// <param name="activeTags">The tags with sufficient empirical evidence.</param>
        /// <returns>The ordered tiers.</returns>
        private static List<IReadOnlyList<string>> ResolveTieredTopologicalOrder(
            Dictionary<string, HashSet<string>> outgoingEdges,
            Dictionary<string, HashSet<string>> incomingEdges,
            IReadOnlyList<string> activeTags)
        {
            List<IReadOnlyList<string>> tiers = new List<IReadOnlyList<string>>();
            HashSet<string> remainingTags =
                new HashSet<string>(activeTags, StringComparer.Ordinal);

            while (remainingTags.Count > 0)
            {
                List<string> currentTier =
                    remainingTags
                        .Where(tag => incomingEdges[tag].Count == 0)
                        .OrderBy(static tag => tag, StringComparer.Ordinal)
                        .ToList();

                if (currentTier.Count == 0)
                {
                    List<string> cycleTier =
                        remainingTags
                            .OrderBy(static tag => tag, StringComparer.Ordinal)
                            .ToList();

                    tiers.Add(cycleTier);
                    break;
                }

                tiers.Add(currentTier);

                foreach (string tag in currentTier)
                {
                    remainingTags.Remove(tag);

                    foreach (string successor in outgoingEdges[tag].ToList())
                    {
                        incomingEdges[successor].Remove(tag);
                    }
                }
            }

            return tiers;
        }

        /// <summary>
        /// Creates an empty adjacency map for all supplied tags.
        /// </summary>
        /// <param name="tags">The tag set to initialize.</param>
        /// <returns>An initialized adjacency map.</returns>
        private static Dictionary<string, HashSet<string>> CreateEmptyAdjacencyMap(
            IReadOnlyList<string> tags)
        {
            Dictionary<string, HashSet<string>> map =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (string tag in tags)
            {
                map.Add(tag, new HashSet<string>(StringComparer.Ordinal));
            }

            return map;
        }

        /// <summary>
        /// Represents the decision outcome for one pairwise relation.
        /// </summary>
        private sealed class PairwiseRelationDecision
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PairwiseRelationDecision"/> class.
            /// </summary>
            /// <param name="isAccepted">Whether the relation was accepted.</param>
            /// <param name="beforeTag">The tag that should come before.</param>
            /// <param name="afterTag">The tag that should come after.</param>
            /// <param name="description">A readable decision description.</param>
            private PairwiseRelationDecision(
                bool isAccepted,
                string? beforeTag,
                string? afterTag,
                string description)
            {
                IsAccepted = isAccepted;
                BeforeTag = beforeTag ?? string.Empty;
                AfterTag = afterTag ?? string.Empty;
                Description = description;
            }

            /// <summary>
            /// Gets a value indicating whether the relation was accepted.
            /// </summary>
            public bool IsAccepted { get; }

            /// <summary>
            /// Gets the tag that should come before.
            /// </summary>
            public string BeforeTag { get; }

            /// <summary>
            /// Gets the tag that should come after.
            /// </summary>
            public string AfterTag { get; }

            /// <summary>
            /// Gets the readable decision description.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Creates an accepted decision.
            /// </summary>
            /// <param name="beforeTag">The tag that should come before.</param>
            /// <param name="afterTag">The tag that should come after.</param>
            /// <param name="description">The decision description.</param>
            /// <returns>The created decision.</returns>
            public static PairwiseRelationDecision CreateAccepted(
                string beforeTag,
                string afterTag,
                string description)
            {
                return new PairwiseRelationDecision(
                    isAccepted: true,
                    beforeTag: beforeTag,
                    afterTag: afterTag,
                    description: description);
            }

            /// <summary>
            /// Creates an ignored decision.
            /// </summary>
            /// <param name="leftTag">The first tag of the compared pair.</param>
            /// <param name="rightTag">The second tag of the compared pair.</param>
            /// <param name="reason">The ignore reason.</param>
            /// <returns>The created decision.</returns>
            public static PairwiseRelationDecision CreateIgnored(
                string leftTag,
                string rightTag,
                string reason)
            {
                return new PairwiseRelationDecision(
                    isAccepted: false,
                    beforeTag: null,
                    afterTag: null,
                    description: leftTag + " vs " + rightTag + " -> " + reason);
            }
        }
    }
}
