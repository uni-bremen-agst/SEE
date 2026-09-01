using System.Threading;

namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents one immutable serialized step fragment of an exception-flow
    /// path deduplication key.
    /// </summary>
    internal sealed class ExceptionFlowPathDeduplicationKeyFragment
    {
        /// <summary>
        /// The polynomial hash multiplier.
        /// </summary>
        private const uint HashMultiplier = 31;

        /// <summary>
        /// Initializes a deduplication-key fragment.
        /// </summary>
        /// <param name="value">The serialized fragment value.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        internal ExceptionFlowPathDeduplicationKeyFragment(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            Value = value;

            uint hashCode = 0;
            uint hashPower = 1;

            unchecked
            {
                foreach (char character in value)
                {
                    hashCode = hashCode * HashMultiplier + character;
                    hashPower *= HashMultiplier;
                }
            }

            HashCode = hashCode;
            HashPower = hashPower;
        }

        /// <summary>
        /// Gets the polynomial hash of the fragment characters.
        /// </summary>
        /// <value>The fragment hash.</value>
        internal uint HashCode { get; }

        /// <summary>
        /// Gets the polynomial multiplier raised to the fragment length.
        /// </summary>
        /// <value>The fragment hash power.</value>
        internal uint HashPower { get; }

        /// <summary>
        /// Gets the serialized fragment value.
        /// </summary>
        /// <value>The serialized fragment.</value>
        internal string Value { get; }
    }

    /// <summary>
    /// Represents an immutable persistent exception-flow path deduplication
    /// key.
    /// </summary>
    /// <remarks>
    /// Hashes use unsigned arithmetic modulo 2^32. For character streams
    /// <c>x</c> and <c>y</c>, the composition
    /// <c>H(x || y) = H(x) * 31^|y| + H(y)</c> makes prepending incremental.
    /// Equality still compares the complete virtual character streams and
    /// never relies on hash identity.
    /// </remarks>
    internal sealed class ExceptionFlowPathDeduplicationKey :
        IEquatable<ExceptionFlowPathDeduplicationKey>
    {
        /// <summary>
        /// Stores the number of characters in the complete logical key.
        /// </summary>
        private readonly int characterLength;

        /// <summary>
        /// Stores the polynomial hash of the complete logical key.
        /// </summary>
        private readonly uint hashCode;

        /// <summary>
        /// Stores the polynomial multiplier raised to the complete logical
        /// key length.
        /// </summary>
        private readonly uint hashPower;

        /// <summary>
        /// Stores the first serialized step fragment.
        /// </summary>
        private readonly ExceptionFlowPathDeduplicationKeyFragment prefix;

        /// <summary>
        /// Stores the persistent suffix, if any.
        /// </summary>
        private readonly ExceptionFlowPathDeduplicationKey? suffix;

        /// <summary>
        /// Stores the lazily materialized complete string representation.
        /// </summary>
        private string? materializedValue;

        /// <summary>
        /// Initializes a persistent path key from one prefix fragment and an
        /// optional existing suffix.
        /// </summary>
        /// <param name="prefix">The first serialized step fragment.</param>
        /// <param name="suffix">The existing suffix, if any.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="prefix"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal ExceptionFlowPathDeduplicationKey(
            ExceptionFlowPathDeduplicationKeyFragment prefix,
            ExceptionFlowPathDeduplicationKey? suffix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            this.prefix = prefix;
            this.suffix = suffix;

            int suffixLength = suffix?.characterLength ?? 0;
            uint suffixHashCode = suffix?.hashCode ?? 0;
            uint suffixHashPower = suffix?.hashPower ?? 1;

            characterLength = prefix.Value.Length + suffixLength;

            unchecked
            {
                hashCode = prefix.HashCode * suffixHashPower + suffixHashCode;
                hashPower = prefix.HashPower * suffixHashPower;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the complete string representation
        /// has been materialized.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the string has been materialized;
        /// otherwise <see langword="false"/>.
        /// </value>
        internal bool IsMaterialized =>
            materializedValue is not null;

        /// <summary>
        /// Gets the complete logical key as a lazily materialized string.
        /// </summary>
        /// <value>The complete serialized key.</value>
        internal string Value
        {
            get
            {
                string? value = Volatile.Read(ref materializedValue);

                if (value is not null)
                {
                    return value;
                }

                string createdValue = string.Create(
                    characterLength,
                    this,
                    static (destination, key) => key.CopyTo(destination));

                Interlocked.CompareExchange(
                    ref materializedValue,
                    createdValue,
                    comparand: null);

                return materializedValue;
            }
        }

        /// <inheritdoc/>
        public bool Equals(ExceptionFlowPathDeduplicationKey? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null
                || characterLength != other.characterLength
                || hashCode != other.hashCode)
            {
                return false;
            }

            CharacterEnumerator left = new(this);
            CharacterEnumerator right = new(other);

            while (left.MoveNext(out char leftCharacter))
            {
                if (!right.MoveNext(out char rightCharacter)
                    || leftCharacter != rightCharacter)
                {
                    return false;
                }
            }

            return !right.MoveNext(out _);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ExceptionFlowPathDeduplicationKey);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return unchecked((int)hashCode);
        }

        /// <summary>
        /// Copies the complete logical character stream to the destination.
        /// </summary>
        /// <param name="destination">The destination character span.</param>
        private void CopyTo(Span<char> destination)
        {
            int destinationIndex = 0;
            ExceptionFlowPathDeduplicationKey? current = this;

            while (current is not null)
            {
                ReadOnlySpan<char> fragment = current.prefix.Value.AsSpan();
                fragment.CopyTo(destination.Slice(destinationIndex));
                destinationIndex += fragment.Length;
                current = current.suffix;
            }
        }

        /// <summary>
        /// Enumerates the characters of a persistent key without materializing
        /// its complete string representation.
        /// </summary>
        private struct CharacterEnumerator
        {
            /// <summary>
            /// Stores the current persistent key node.
            /// </summary>
            private ExceptionFlowPathDeduplicationKey? current;

            /// <summary>
            /// Stores the next character offset in the current fragment.
            /// </summary>
            private int offset;

            /// <summary>
            /// Initializes a character enumerator.
            /// </summary>
            /// <param name="key">The key to enumerate.</param>
            public CharacterEnumerator(ExceptionFlowPathDeduplicationKey key)
            {
                current = key;
                offset = 0;
            }

            /// <summary>
            /// Advances to the next character.
            /// </summary>
            /// <param name="character">
            /// Receives the next character when one is available.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if a character was returned; otherwise
            /// <see langword="false"/>.
            /// </returns>
            public bool MoveNext(out char character)
            {
                while (current is not null)
                {
                    string fragment = current.prefix.Value;

                    if (offset < fragment.Length)
                    {
                        character = fragment[offset];
                        offset++;
                        return true;
                    }

                    current = current.suffix;
                    offset = 0;
                }

                character = default;
                return false;
            }
        }
    }
}
