using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one PDB checksum recorded in a PE debug directory.
    /// </summary>
    internal sealed class ExternalPdbChecksum : IEquatable<ExternalPdbChecksum>
    {
        /// <summary>
        /// Initializes a PDB checksum descriptor.
        /// </summary>
        /// <param name="algorithmName">The recorded checksum algorithm name.</param>
        /// <param name="checksum">The recorded checksum bytes.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="algorithmName"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExternalPdbChecksum(
            string algorithmName,
            ImmutableArray<byte> checksum)
        {
            ArgumentNullException.ThrowIfNull(algorithmName);

            AlgorithmName = algorithmName;
            Checksum = checksum.IsDefault ? ImmutableArray<byte>.Empty : checksum;
        }

        /// <summary>
        /// Gets the recorded checksum algorithm name.
        /// </summary>
        /// <value>The checksum algorithm name.</value>
        public string AlgorithmName { get; }

        /// <summary>
        /// Gets the recorded checksum bytes.
        /// </summary>
        /// <value>The immutable checksum byte sequence.</value>
        public ImmutableArray<byte> Checksum { get; }

        /// <summary>
        /// Determines whether another checksum has the same algorithm and
        /// byte sequence.
        /// </summary>
        /// <param name="other">The checksum to compare.</param>
        /// <returns>
        /// <see langword="true"/> when both values are equal; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(ExternalPdbChecksum? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return string.Equals(AlgorithmName, other.AlgorithmName, StringComparison.Ordinal)
                && Checksum.SequenceEqual(other.Checksum);
        }

        /// <summary>
        /// Determines whether another object represents the same checksum.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// <see langword="true"/> when the object is an equal checksum;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ExternalPdbChecksum);
        }

        /// <summary>
        /// Gets a sequence-based hash code for the checksum.
        /// </summary>
        /// <returns>The value-based hash code.</returns>
        public override int GetHashCode()
        {
            HashCode hashCode = new();
            hashCode.Add(AlgorithmName, StringComparer.Ordinal);

            foreach (byte value in Checksum)
            {
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }
    }
}
