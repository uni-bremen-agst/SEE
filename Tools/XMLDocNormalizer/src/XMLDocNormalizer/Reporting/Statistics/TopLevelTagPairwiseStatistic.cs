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
        /// <value>
        /// The left tag of the pair.
        /// </value>
        public string LeftTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the right tag of the pair.
        /// </summary>
        /// <value>
        /// The right tag of the pair.
        /// </value>
        public string RightTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of observations in which both tags were present.
        /// </summary>
        /// <value>
        /// The total number of observations in which both tags were present.
        /// </value>
        public int TotalComparableObservations { get; set; }

        /// <summary>
        /// Gets or sets the number of observations in which the left tag appeared before the right tag.
        /// </summary>
        /// <value>
        /// The number of observations in which the left tag appeared before the right tag.
        /// </value>
        public int LeftBeforeRightCount { get; set; }

        /// <summary>
        /// Gets or sets the number of observations in which the right tag appeared before the left tag.
        /// </summary>
        /// <value>
        /// The number of observations in which the right tag appeared before the left tag.
        /// </value>
        public int RightBeforeLeftCount { get; set; }

        /// <summary>
        /// Gets the percentage of comparable observations in which the left tag appeared before the right tag.
        /// </summary>
        /// <value>
        /// The percentage of comparable observations in which the left tag appeared before the right tag.
        /// </value>
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
        /// <value>
        /// The percentage of comparable observations in which the right tag appeared before the left tag.
        /// </value>
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
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="other"/> is null.
        /// </exception>
        public void Merge(TopLevelTagPairwiseStatistic other)
        {
            ArgumentNullException.ThrowIfNull(other);

            TotalComparableObservations += other.TotalComparableObservations;
            LeftBeforeRightCount += other.LeftBeforeRightCount;
            RightBeforeLeftCount += other.RightBeforeLeftCount;
        }
    }
}
