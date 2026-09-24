namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies a source declaration without retaining a Roslyn syntax object.
    /// </summary>
    internal sealed class CanonicalSourceLocationIdentity
        : IEquatable<CanonicalSourceLocationIdentity>
    {
        /// <summary>
        /// Initializes a canonical source location.
        /// </summary>
        /// <param name="documentName">The logical syntax-tree document name.</param>
        /// <param name="checksumAlgorithm">The source checksum algorithm.</param>
        /// <param name="checksum">The uppercase source checksum.</param>
        /// <param name="spanStart">The declaration span start.</param>
        /// <param name="spanLength">The declaration span length.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalSourceLocationIdentity(
            string documentName,
            string checksumAlgorithm,
            string checksum,
            int spanStart,
            int spanLength)
        {
            ArgumentNullException.ThrowIfNull(documentName);
            ArgumentException.ThrowIfNullOrWhiteSpace(checksumAlgorithm);
            ArgumentNullException.ThrowIfNull(checksum);

            DocumentName = documentName;
            ChecksumAlgorithm = checksumAlgorithm;
            Checksum = checksum.ToUpperInvariant();
            SpanStart = spanStart;
            SpanLength = spanLength;
        }

        /// <summary>
        /// Gets the logical source document name.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string DocumentName { get; }

        /// <summary>
        /// Gets the source checksum algorithm.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string ChecksumAlgorithm { get; }

        /// <summary>
        /// Gets the uppercase source checksum.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string Checksum { get; }

        /// <summary>
        /// Gets the declaration span start.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int SpanStart { get; }

        /// <summary>
        /// Gets the declaration span length.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int SpanLength { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalSourceLocationIdentity? other)
        {
            return other != null
                && StringComparer.Ordinal.Equals(DocumentName, other.DocumentName)
                && StringComparer.Ordinal.Equals(ChecksumAlgorithm, other.ChecksumAlgorithm)
                && StringComparer.Ordinal.Equals(Checksum, other.Checksum)
                && SpanStart == other.SpanStart
                && SpanLength == other.SpanLength;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalSourceLocationIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(DocumentName, StringComparer.Ordinal);
            hash.Add(ChecksumAlgorithm, StringComparer.Ordinal);
            hash.Add(Checksum, StringComparer.Ordinal);
            hash.Add(SpanStart);
            hash.Add(SpanLength);
            return hash.ToHashCode();
        }
    }
}
