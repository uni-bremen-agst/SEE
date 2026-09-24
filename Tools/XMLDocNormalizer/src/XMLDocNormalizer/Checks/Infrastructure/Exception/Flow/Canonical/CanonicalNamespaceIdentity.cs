namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Represents an immutable namespace declaration identity within one assembly.
    /// </summary>
    internal sealed class CanonicalNamespaceIdentity
        : IEquatable<CanonicalNamespaceIdentity>
    {
        /// <summary>
        /// Initializes a canonical namespace identity.
        /// </summary>
        /// <param name="assembly">The declaring assembly identity.</param>
        /// <param name="fullName">The dot-separated namespace name.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalNamespaceIdentity(
            CanonicalAssemblyIdentity assembly,
            string fullName)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(fullName);

            Assembly = assembly;
            FullName = fullName;
        }

        /// <summary>
        /// Gets the declaring assembly identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalAssemblyIdentity Assembly { get; }

        /// <summary>
        /// Gets the full metadata namespace name, or an empty string for global.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string FullName { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalNamespaceIdentity? other)
        {
            return other != null
                && Assembly.Equals(other.Assembly)
                && StringComparer.Ordinal.Equals(FullName, other.FullName);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalNamespaceIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Assembly,
                StringComparer.Ordinal.GetHashCode(FullName));
        }
    }
}
