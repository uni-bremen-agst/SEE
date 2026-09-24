using System.Globalization;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies the metadata content represented by an assembly identity.
    /// </summary>
    internal enum CanonicalAssemblyContentType
    {
        /// <summary>
        /// The assembly contains ordinary managed code.
        /// </summary>
        Default,

        /// <summary>
        /// The assembly contains Windows Runtime metadata.
        /// </summary>
        WindowsRuntime
    }

    /// <summary>
    /// Represents an immutable, Roslyn-independent assembly identity.
    /// </summary>
    /// <remarks>
    /// The public key or token is stored as deterministic hexadecimal text,
    /// but remains a distinct structured identity component rather than an
    /// assembly display name.
    /// </remarks>
    internal sealed class CanonicalAssemblyIdentity
        : IEquatable<CanonicalAssemblyIdentity>
    {
        /// <summary>
        /// Initializes a canonical assembly identity.
        /// </summary>
        /// <param name="name">The simple assembly name.</param>
        /// <param name="versionMajor">The major version component.</param>
        /// <param name="versionMinor">The minor version component.</param>
        /// <param name="versionBuild">The build version component.</param>
        /// <param name="versionRevision">The revision version component.</param>
        /// <param name="cultureName">The normalized culture name.</param>
        /// <param name="publicKeyOrToken">The uppercase public-key or token bytes.</param>
        /// <param name="hasPublicKey">
        /// Whether <paramref name="publicKeyOrToken"/> is a full public key.
        /// </param>
        /// <param name="isRetargetable">Whether the assembly is retargetable.</param>
        /// <param name="contentType">The assembly content type.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalAssemblyIdentity(
            string name,
            int versionMajor,
            int versionMinor,
            int versionBuild,
            int versionRevision,
            string cultureName,
            string publicKeyOrToken,
            bool hasPublicKey,
            bool isRetargetable,
            CanonicalAssemblyContentType contentType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(cultureName);
            ArgumentNullException.ThrowIfNull(publicKeyOrToken);

            Name = name;
            VersionMajor = versionMajor;
            VersionMinor = versionMinor;
            VersionBuild = versionBuild;
            VersionRevision = versionRevision;
            CultureName = cultureName;
            PublicKeyOrToken = publicKeyOrToken.ToUpperInvariant();
            HasPublicKey = hasPublicKey;
            IsRetargetable = isRetargetable;
            ContentType = contentType;
        }

        /// <summary>
        /// Gets the simple assembly name.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the major version component.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int VersionMajor { get; }

        /// <summary>
        /// Gets the minor version component.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int VersionMinor { get; }

        /// <summary>
        /// Gets the build version component.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int VersionBuild { get; }

        /// <summary>
        /// Gets the revision version component.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int VersionRevision { get; }

        /// <summary>
        /// Gets the normalized culture name, or an empty string for neutral culture.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string CultureName { get; }

        /// <summary>
        /// Gets the uppercase hexadecimal public key or public-key token.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string PublicKeyOrToken { get; }

        /// <summary>
        /// Gets whether <see cref="PublicKeyOrToken"/> is a full public key.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool HasPublicKey { get; }

        /// <summary>
        /// Gets whether the assembly identity is retargetable.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool IsRetargetable { get; }

        /// <summary>
        /// Gets the assembly content type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalAssemblyContentType ContentType { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalAssemblyIdentity? other)
        {
            return other != null
                && StringComparer.Ordinal.Equals(Name, other.Name)
                && VersionMajor == other.VersionMajor
                && VersionMinor == other.VersionMinor
                && VersionBuild == other.VersionBuild
                && VersionRevision == other.VersionRevision
                && StringComparer.Ordinal.Equals(CultureName, other.CultureName)
                && StringComparer.Ordinal.Equals(PublicKeyOrToken, other.PublicKeyOrToken)
                && HasPublicKey == other.HasPublicKey
                && IsRetargetable == other.IsRetargetable
                && ContentType == other.ContentType;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalAssemblyIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Name, StringComparer.Ordinal);
            hash.Add(VersionMajor);
            hash.Add(VersionMinor);
            hash.Add(VersionBuild);
            hash.Add(VersionRevision);
            hash.Add(CultureName, StringComparer.Ordinal);
            hash.Add(PublicKeyOrToken, StringComparer.Ordinal);
            hash.Add(HasPublicKey);
            hash.Add(IsRetargetable);
            hash.Add(ContentType);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Creates a deterministic diagnostic representation that is not used
        /// as the authoritative identity.
        /// </summary>
        /// <returns>The diagnostic representation.</returns>
        public override string ToString()
        {
            string version = string.Join(
                ".",
                VersionMajor.ToString(CultureInfo.InvariantCulture),
                VersionMinor.ToString(CultureInfo.InvariantCulture),
                VersionBuild.ToString(CultureInfo.InvariantCulture),
                VersionRevision.ToString(CultureInfo.InvariantCulture));

            return $"{Name}, Version={version}, Culture={CultureName}, Key={PublicKeyOrToken}";
        }
    }
}
