using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Captures the immutable opt-in NuGet V3 feed, bounded package hints,
    /// optional assembly-name search, and hard acquisition limits.
    /// </summary>
    internal sealed class ExternalRemoteReferenceAcquisitionConfiguration
    {
        /// <summary>Initializes a validated configuration snapshot.</summary>
        /// <param name="policy">The explicit context-local acquisition policy.</param>
        /// <param name="serviceIndexUri">The safe NuGet V3 service-index URI.</param>
        /// <param name="packageHints">The immutable bounded discovery hints.</param>
        /// <param name="enablePackageSearch">Whether bounded package search is enabled.</param>
        /// <param name="limits">The immutable hard-limit snapshot.</param>
        private ExternalRemoteReferenceAcquisitionConfiguration(
            ExternalReferenceAcquisitionPolicy policy,
            Uri serviceIndexUri,
            ImmutableArray<ExternalRemoteReferencePackageHint> packageHints,
            bool enablePackageSearch,
            ExternalRemoteReferenceAcquisitionLimits limits)
        {
            Policy = policy;
            ServiceIndexUri = serviceIndexUri;
            PackageHints = packageHints;
            EnablePackageSearch = enablePackageSearch;
            Limits = limits;
        }

        /// <summary>Gets the per-context reference acquisition policy.</summary>
        /// <value>The explicit acquisition policy.</value>
        public ExternalReferenceAcquisitionPolicy Policy { get; }

        /// <summary>Gets the configured NuGet V3 service index.</summary>
        /// <value>The safe absolute service-index URI.</value>
        public Uri ServiceIndexUri { get; }

        /// <summary>Gets deterministic explicit package/version discovery hints.</summary>
        /// <value>The immutable discovery-hint sequence.</value>
        public ImmutableArray<ExternalRemoteReferencePackageHint> PackageHints { get; }

        /// <summary>Gets whether bounded assembly-name NuGet search is enabled.</summary>
        /// <value><see langword="true"/> when bounded package search is enabled.</value>
        public bool EnablePackageSearch { get; }

        /// <summary>Gets the complete hard-limit snapshot.</summary>
        /// <value>The immutable acquisition limits.</value>
        public ExternalRemoteReferenceAcquisitionLimits Limits { get; }

        /// <summary>Creates a validated immutable configuration without network access.</summary>
        /// <param name="policy">The explicit context-local acquisition policy.</param>
        /// <param name="serviceIndexUri">The candidate NuGet V3 service-index URI.</param>
        /// <param name="packageHints">The candidate bounded discovery hints.</param>
        /// <param name="enablePackageSearch">Whether bounded package search is enabled.</param>
        /// <param name="limits">The candidate hard-limit snapshot.</param>
        /// <param name="configuration">The validated immutable configuration.</param>
        /// <returns><see langword="true"/> when every configuration value is valid.</returns>
        public static bool TryCreate(
            ExternalReferenceAcquisitionPolicy policy,
            Uri serviceIndexUri,
            IEnumerable<ExternalRemoteReferencePackageHint> packageHints,
            bool enablePackageSearch,
            ExternalRemoteReferenceAcquisitionLimits limits,
            out ExternalRemoteReferenceAcquisitionConfiguration configuration)
        {
            if (serviceIndexUri == null
                || packageHints == null
                || limits == null
                || !Enum.IsDefined(policy)
                || !limits.IsValid()
                || !IsSafeFeedUri(serviceIndexUri))
            {
                configuration = null!;
                return false;
            }

            ImmutableArray<ExternalRemoteReferencePackageHint> hints;
            try
            {
                hints = packageHints
                    .OrderBy(static hint => hint.AssemblySimpleName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static hint => hint.PackageId, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
            }
            catch (ArgumentNullException)
            {
                configuration = null!;
                return false;
            }

            if (hints.Any(static hint => hint == null))
            {
                configuration = null!;
                return false;
            }

            configuration = new ExternalRemoteReferenceAcquisitionConfiguration(
                policy,
                serviceIndexUri,
                hints,
                enablePackageSearch,
                limits);
            return true;
        }

        /// <summary>Returns whether two immutable configuration snapshots are equal.</summary>
        /// <param name="other">The configuration snapshot to compare.</param>
        /// <returns><see langword="true"/> when all configuration values are equivalent.</returns>
        public bool IsEquivalentTo(ExternalRemoteReferenceAcquisitionConfiguration other)
        {
            return other != null
                && Policy == other.Policy
                && ServiceIndexUri == other.ServiceIndexUri
                && EnablePackageSearch == other.EnablePackageSearch
                && Limits == other.Limits
                && PackageHints.Length == other.PackageHints.Length
                && PackageHints.Zip(other.PackageHints).All(static pair =>
                    pair.First.AssemblySimpleName == pair.Second.AssemblySimpleName
                    && pair.First.PackageId == pair.Second.PackageId
                    && pair.First.ProviderKind == pair.Second.ProviderKind
                    && pair.First.Evidence == pair.Second.Evidence
                    && pair.First.CandidateVersions.SequenceEqual(
                        pair.Second.CandidateVersions,
                        StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Determines whether a feed URI satisfies static transport constraints.</summary>
        /// <param name="uri">The candidate feed URI.</param>
        /// <returns><see langword="true"/> for an absolute credential-free HTTPS URI.</returns>
        private static bool IsSafeFeedUri(Uri uri)
        {
            return uri.IsAbsoluteUri
                && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && uri.UserInfo.Length == 0
                && uri.Fragment.Length == 0;
        }
    }
}
