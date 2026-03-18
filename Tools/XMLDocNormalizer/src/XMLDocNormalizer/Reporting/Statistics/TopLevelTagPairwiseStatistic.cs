namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Represents one pairwise ordering statistic between two top-level XML documentation tags.
    /// </summary>
    internal sealed class TopLevelTagPairwiseStatistic
    {
        /// <summary>
        /// Gets or sets the left tag of the pair.
        /// </summary>
        public string LeftTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the right tag of the pair.
        /// </summary>
        public string RightTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of observations in which both tags were present.
        /// </summary>
        public int TotalComparableObservations { get; set; }

        /// <summary>
        /// Gets or sets the number of observations in which the left tag appeared before the right tag.
        /// </summary>
        public int LeftBeforeRightCount { get; set; }

        /// <summary>
        /// Gets or sets the number of observations in which the right tag appeared before the left tag.
        /// </summary>
        public int RightBeforeLeftCount { get; set; }

        /// <summary>
        /// Gets the percentage of comparable observations in which the left tag appeared before the right tag.
        /// </summary>
        public double LeftBeforeRightPercentage
        {
            get
            {
                if (TotalComparableObservations == 0)
                {
                    return 0.0;
                }

                return LeftBeforeRightCount * 100.0 / TotalComparableObservations;
            }
        }

        /// <summary>
        /// Gets the percentage of comparable observations in which the right tag appeared before the left tag.
        /// </summary>
        public double RightBeforeLeftPercentage
        {
            get
            {
                if (TotalComparableObservations == 0)
                {
                    return 0.0;
                }

                return RightBeforeLeftCount * 100.0 / TotalComparableObservations;
            }
        }

        /// <summary>
        /// Merges another pairwise statistic into this instance.
        /// </summary>
        /// <param name="other">The statistic to merge in.</param>
        public void Merge(TopLevelTagPairwiseStatistic other)
        {
            ArgumentNullException.ThrowIfNull(other);

            TotalComparableObservations += other.TotalComparableObservations;
            LeftBeforeRightCount += other.LeftBeforeRightCount;
            RightBeforeLeftCount += other.RightBeforeLeftCount;
        }
    }
}
