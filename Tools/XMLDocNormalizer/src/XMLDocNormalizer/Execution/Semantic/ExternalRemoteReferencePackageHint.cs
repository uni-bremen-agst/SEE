using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Maps a non-authoritative assembly-name hint to a bounded package and
    /// explicit candidate-version sequence.
    /// </summary>
    internal sealed record ExternalRemoteReferencePackageHint
    {
        /// <summary>Initializes one validated immutable discovery hint.</summary>
        /// <param name="assemblySimpleName">The assembly simple name or wildcard.</param>
        /// <param name="packageId">The safe package identifier.</param>
        /// <param name="candidateVersions">The bounded explicit version sequence.</param>
        /// <param name="providerKind">The artifact provider classification.</param>
        /// <param name="evidence">The human-readable discovery evidence.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value or sequence is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a required value is empty or contains unsafe syntax.
        /// </exception>
        public ExternalRemoteReferencePackageHint(
            string assemblySimpleName,
            string packageId,
            IEnumerable<string> candidateVersions,
            ExternalRemoteArtifactProviderKind providerKind,
            string evidence)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblySimpleName);
            ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
            ArgumentNullException.ThrowIfNull(candidateVersions);
            ArgumentException.ThrowIfNullOrWhiteSpace(evidence);

            if (!(assemblySimpleName == "*" || IsSafeIdentifier(assemblySimpleName))
                || !IsSafeIdentifier(packageId)
                || !Enum.IsDefined(providerKind))
            {
                throw new ArgumentException("Package hints contain an unsafe identifier.");
            }

            ImmutableArray<string> versions = candidateVersions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();
            if (versions.Length == 0 || versions.Any(static version => !IsSafeVersion(version)))
            {
                throw new ArgumentException("Package hints require safe explicit versions.");
            }

            AssemblySimpleName = assemblySimpleName;
            PackageId = packageId;
            CandidateVersions = versions;
            ProviderKind = providerKind;
            Evidence = evidence;
        }

        /// <summary>Gets the assembly simple-name discovery hint.</summary>
        /// <value>The assembly simple name or wildcard.</value>
        public string AssemblySimpleName { get; }

        /// <summary>Gets the package identifier discovery hint.</summary>
        /// <value>The safe package identifier.</value>
        public string PackageId { get; }

        /// <summary>Gets the deterministic bounded version candidate sequence.</summary>
        /// <value>The immutable candidate-version sequence.</value>
        public ImmutableArray<string> CandidateVersions { get; }

        /// <summary>Gets the provider classification used for provenance and statistics.</summary>
        /// <value>The provider classification.</value>
        public ExternalRemoteArtifactProviderKind ProviderKind { get; }

        /// <summary>Gets the human-readable provenance evidence for this hint.</summary>
        /// <value>The non-authoritative discovery evidence.</value>
        public string Evidence { get; }

        /// <summary>Determines whether a package component has safe bounded syntax.</summary>
        /// <param name="value">The candidate package component.</param>
        /// <returns><see langword="true"/> when the component is safe.</returns>
        private static bool IsSafeIdentifier(string value)
        {
            return value.Length <= 256
                && value.All(static character => char.IsAsciiLetterOrDigit(character)
                    || character is '.' or '-' or '_');
        }

        /// <summary>Determines whether a package version has safe bounded syntax.</summary>
        /// <param name="value">The candidate package version.</param>
        /// <returns><see langword="true"/> when the version is safe.</returns>
        private static bool IsSafeVersion(string value)
        {
            return value.Length is > 0 and <= 128
                && value.All(static character => char.IsAsciiLetterOrDigit(character)
                    || character is '.' or '-' or '+');
        }
    }
}
