namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Stores hard bounds for one context-local remote reference run.</summary>
    internal sealed record ExternalRemoteReferenceAcquisitionLimits
    {
        /// <summary>Gets the conservative production defaults.</summary>
        /// <value>The shared immutable default limit snapshot.</value>
        public static ExternalRemoteReferenceAcquisitionLimits Default { get; } = new();

        /// <summary>Gets the maximum search results considered per reference.</summary>
        /// <value>The positive search-result limit.</value>
        public int MaxSearchResultsPerReference { get; init; } = 3;

        /// <summary>Gets the maximum versions considered for one package.</summary>
        /// <value>The positive package-version limit.</value>
        public int MaxPackageVersionsPerPackage { get; init; } = 3;

        /// <summary>Gets the maximum package artifacts downloaded per reference.</summary>
        /// <value>The positive per-reference package limit.</value>
        public int MaxPackagesDownloadedPerReference { get; init; } = 4;

        /// <summary>Gets the maximum HTTP requests made per reference.</summary>
        /// <value>The positive per-reference request limit.</value>
        public int MaxRequestsPerReference { get; init; } = 12;

        /// <summary>Gets the maximum HTTP requests made by the context.</summary>
        /// <value>The positive context-wide request limit.</value>
        public int MaxRequestsPerRun { get; init; } = 128;

        /// <summary>Gets the maximum downloaded response bytes per reference.</summary>
        /// <value>The positive per-reference byte limit.</value>
        public int MaxDownloadedBytesPerReference { get; init; } = 32 * 1024 * 1024;

        /// <summary>Gets the maximum downloaded response bytes for the context.</summary>
        /// <value>The positive context-wide byte limit.</value>
        public int MaxDownloadedBytesPerRun { get; init; } = 128 * 1024 * 1024;

        /// <summary>Gets the maximum size of one compressed package response.</summary>
        /// <value>The positive artifact byte limit.</value>
        public int MaxArtifactBytes { get; init; } = 16 * 1024 * 1024;

        /// <summary>Gets the maximum decompressed size of one binary entry.</summary>
        /// <value>The positive archive-entry byte limit.</value>
        public int MaxArchiveEntryBytes { get; init; } = 16 * 1024 * 1024;

        /// <summary>Gets the maximum candidate entries inspected in one package.</summary>
        /// <value>The positive candidate-entry limit.</value>
        public int MaxBinaryCandidatesPerArtifact { get; init; } = 256;

        /// <summary>Gets the total timeout for one HTTP request chain.</summary>
        /// <value>The positive request-chain timeout.</value>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);

        /// <summary>Determines whether every configured bound is positive.</summary>
        /// <returns><see langword="true"/> when every configured bound is positive.</returns>
        public bool IsValid()
        {
            return MaxSearchResultsPerReference > 0
                && MaxPackageVersionsPerPackage > 0
                && MaxPackagesDownloadedPerReference > 0
                && MaxRequestsPerReference > 0
                && MaxRequestsPerRun > 0
                && MaxDownloadedBytesPerReference > 0
                && MaxDownloadedBytesPerRun > 0
                && MaxArtifactBytes > 0
                && MaxArchiveEntryBytes > 0
                && MaxBinaryCandidatesPerArtifact > 0
                && Timeout > TimeSpan.Zero;
        }
    }
}
