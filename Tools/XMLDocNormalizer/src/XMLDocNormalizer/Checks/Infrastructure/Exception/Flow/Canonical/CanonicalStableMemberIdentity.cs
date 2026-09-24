namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies a stable member kind supported by exception-flow call contexts.
    /// </summary>
    internal enum CanonicalStableMemberKind
    {
        /// <summary>
        /// A field member.
        /// </summary>
        Field,

        /// <summary>
        /// A property or indexer member.
        /// </summary>
        Property
    }

    /// <summary>
    /// Represents a Roslyn-independent field or property identity that can be
    /// retained in a call context.
    /// </summary>
    internal sealed class CanonicalStableMemberIdentity
        : IEquatable<CanonicalStableMemberIdentity>
    {
        /// <summary>
        /// Stores property parameter identities.
        /// </summary>
        private readonly CanonicalCallableParameterIdentity[] parameters;

        /// <summary>
        /// Initializes a canonical stable-member identity.
        /// </summary>
        /// <param name="containingType">The containingType value.</param>
        /// <param name="metadataName">The metadataName value.</param>
        /// <param name="kind">The kind value.</param>
        /// <param name="memberType">The memberType value.</param>
        /// <param name="isStatic">The isStatic value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalStableMemberIdentity(
            CanonicalTypeIdentity containingType,
            string metadataName,
            CanonicalStableMemberKind kind,
            CanonicalTypeIdentity memberType,
            bool isStatic,
            CanonicalCallableParameterIdentity[]? parameters)
        {
            ArgumentNullException.ThrowIfNull(containingType);
            ArgumentException.ThrowIfNullOrWhiteSpace(metadataName);
            ArgumentNullException.ThrowIfNull(memberType);

            ContainingType = containingType;
            MetadataName = metadataName;
            Kind = kind;
            MemberType = memberType;
            IsStatic = isStatic;
            this.parameters = parameters?.ToArray()
                ?? Array.Empty<CanonicalCallableParameterIdentity>();
        }

        /// <summary>
        /// Gets the containing type identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity ContainingType { get; }

        /// <summary>
        /// Gets the metadata member name.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string MetadataName { get; }

        /// <summary>
        /// Gets the stable-member kind.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalStableMemberKind Kind { get; }

        /// <summary>
        /// Gets the field or property value type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity MemberType { get; }

        /// <summary>
        /// Gets whether the member is static.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool IsStatic { get; }

        /// <summary>
        /// Gets a copy of the property or indexer parameter identities.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableParameterIdentity[] Parameters => parameters.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalStableMemberIdentity? other)
        {
            return other != null
                && ContainingType.Equals(other.ContainingType)
                && StringComparer.Ordinal.Equals(MetadataName, other.MetadataName)
                && Kind == other.Kind
                && MemberType.Equals(other.MemberType)
                && IsStatic == other.IsStatic
                && parameters.SequenceEqual(other.parameters);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalStableMemberIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(ContainingType);
            hash.Add(MetadataName, StringComparer.Ordinal);
            hash.Add(Kind);
            hash.Add(MemberType);
            hash.Add(IsStatic);

            foreach (CanonicalCallableParameterIdentity parameter in parameters)
            {
                hash.Add(parameter);
            }

            return hash.ToHashCode();
        }
    }
}
