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
        public List<IReadOnlyList<string>> OrderedTiers { get; } =
            new List<IReadOnlyList<string>>();

        /// <summary>
        /// Gets the accepted directional relations that contributed to the ordering.
        /// </summary>
        public List<string> AcceptedRelations { get; } =
            new List<string>();

        /// <summary>
        /// Gets the ignored weak or inconclusive relations.
        /// </summary>
        public List<string> IgnoredRelations { get; } =
            new List<string>();

        /// <summary>
        /// Gets the tags for which insufficient evidence was available to place them in the empirical order.
        /// </summary>
        public List<string> InactiveTags { get; } =
            new List<string>();

        /// <summary>
        /// Gets the final order as a readable string.
        /// Tiers with multiple tags are enclosed in parentheses.
        /// </summary>
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
