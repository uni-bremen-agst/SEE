namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Represents one observed top-level XML documentation tag sequence.
    /// </summary>
    internal sealed class TopLevelTagOrderObservation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevelTagOrderObservation"/> class.
        /// </summary>
        /// <param name="memberKind">The documented member kind.</param>
        /// <param name="rawSequence">The raw top-level tag sequence as observed in source order.</param>
        /// <param name="collapsedSequence">
        /// The normalized top-level tag sequence in which consecutive repeatable tags are collapsed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="memberKind"/>,
        /// <paramref name="rawSequence"/>, or
        /// <paramref name="collapsedSequence"/> is null.
        /// </exception>
        public TopLevelTagOrderObservation(
            string memberKind,
            IReadOnlyList<string> rawSequence,
            IReadOnlyList<string> collapsedSequence)
        {
            ArgumentNullException.ThrowIfNull(memberKind);
            ArgumentNullException.ThrowIfNull(rawSequence);
            ArgumentNullException.ThrowIfNull(collapsedSequence);

            MemberKind = memberKind;
            RawSequence = rawSequence;
            CollapsedSequence = collapsedSequence;
        }

        /// <summary>
        /// Gets the documented member kind.
        /// </summary>
        /// <value>
        /// The documented member kind.
        /// </value>
        public string MemberKind { get; }

        /// <summary>
        /// Gets the raw top-level tag sequence.
        /// </summary>
        /// <value>
        /// The raw top-level tag sequence as observed in source order.
        /// </value>
        public IReadOnlyList<string> RawSequence { get; }

        /// <summary>
        /// Gets the normalized top-level tag sequence.
        /// </summary>
        /// <value>
        /// The normalized top-level tag sequence in which consecutive repeatable tags are collapsed.
        /// </value>
        public IReadOnlyList<string> CollapsedSequence { get; }

        /// <summary>
        /// Gets the raw sequence as a stable string key.
        /// </summary>
        /// <value>
        /// The raw sequence represented as a stable string key.
        /// </value>
        public string RawSequenceKey
        {
            get
            {
                return string.Join(" > ", RawSequence);
            }
        }

        /// <summary>
        /// Gets the normalized sequence as a stable string key.
        /// </summary>
        /// <value>
        /// The normalized sequence represented as a stable string key.
        /// </value>
        public string CollapsedSequenceKey
        {
            get
            {
                return string.Join(" > ", CollapsedSequence);
            }
        }

        /// <summary>
        /// Determines whether the normalized sequence contains a specific tag.
        /// </summary>
        /// <param name="tagName">The tag name to search for.</param>
        /// <returns>True, if the tag is present; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="tagName"/> is null.
        /// </exception>
        public bool HasTag(string tagName)
        {
            ArgumentNullException.ThrowIfNull(tagName);

            for (int i = 0; i < CollapsedSequence.Count; i++)
            {
                if (CollapsedSequence[i] == tagName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
