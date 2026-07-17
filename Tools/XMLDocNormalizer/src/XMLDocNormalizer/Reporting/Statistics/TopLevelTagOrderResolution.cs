using System.Collections.Generic;

namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Represents an empirically derived ordering of top-level XML documentation tags.
    /// </summary>
    internal sealed class TopLevelTagOrderResolution
    {
        /// <summary>
        /// Gets the ordered tag tiers.
        /// Tags within the same tier could not be separated with sufficient confidence.
        /// </summary>
        /// <value>
        /// The ordered tag tiers derived from accepted pairwise relations.
        /// </value>
        public List<IReadOnlyList<string>> OrderedTiers { get; } =
            new List<IReadOnlyList<string>>();

        /// <summary>
        /// Gets the accepted directional relations that contributed to the ordering.
        /// </summary>
        /// <value>
        /// The accepted directional relations that contributed to the ordering.
        /// </value>
        public List<string> AcceptedRelations { get; } =
            new List<string>();

        /// <summary>
        /// Gets the ignored weak or inconclusive relations.
        /// </summary>
        /// <value>
        /// The ignored weak or inconclusive relations.
        /// </value>
        public List<string> IgnoredRelations { get; } =
            new List<string>();

        /// <summary>
        /// Gets the tags for which insufficient evidence was available to place them in the empirical order.
        /// </summary>
        /// <value>
        /// The tags for which insufficient evidence was available.
        /// </value>
        public List<string> InactiveTags { get; } =
            new List<string>();

        /// <summary>
        /// Gets the final order as a readable string.
        /// Tiers with multiple tags are enclosed in parentheses.
        /// </summary>
        /// <value>
        /// The final resolved order represented as a readable string.
        /// </value>
        public string ReadableOrder
        {
            get
            {
                List<string> parts = new List<string>();

                foreach (IReadOnlyList<string> tier in OrderedTiers)
                {
                    if (tier.Count == 1)
                    {
                        parts.Add(tier[0]);
                        continue;
                    }

                    parts.Add("(" + string.Join(", ", tier) + ")");
                }

                return string.Join(" > ", parts);
            }
        }
    }
}
