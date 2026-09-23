using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Captures bounded remote reference work by provider.</summary>
    /// <param name="ProviderKind">The provider represented by the snapshot.</param>
    /// <param name="SearchCount">The number of bounded searches.</param>
    /// <param name="MetadataRequestCount">The number of metadata requests.</param>
    /// <param name="ArtifactRequestCount">The number of artifact requests.</param>
    /// <param name="DownloadedBytes">The downloaded response bytes.</param>
    /// <param name="CandidateBinaryCount">The candidate binaries inspected.</param>
    /// <param name="P5ValidationAttemptCount">The unchanged P5 validation attempts.</param>
    /// <param name="RemoteExactCount">The exact remote materials accepted.</param>
    /// <param name="RejectedCount">The candidates or artifacts rejected.</param>
    /// <param name="UnavailableCount">The references left unavailable.</param>
    /// <param name="CacheHits">The positive and negative cache hits.</param>
    /// <param name="LimitHits">The hard-limit stops.</param>
    /// <param name="DurationTicks">The acquisition duration in stopwatch ticks.</param>
    internal sealed record ExternalRemoteArtifactProviderStatistics(
        ExternalRemoteArtifactProviderKind ProviderKind,
        long SearchCount,
        long MetadataRequestCount,
        long ArtifactRequestCount,
        long DownloadedBytes,
        long CandidateBinaryCount,
        long P5ValidationAttemptCount,
        long RemoteExactCount,
        long RejectedCount,
        long UnavailableCount,
        long CacheHits,
        long LimitHits,
        long DurationTicks);

    /// <summary>Captures aggregate context-local bounded remote acquisition work.</summary>
    /// <param name="RemoteSearchCount">The total bounded searches.</param>
    /// <param name="RemoteRequestCount">The total HTTP requests.</param>
    /// <param name="RemoteArtifactRequestCount">The total artifact requests.</param>
    /// <param name="RemoteDownloadedBytes">The total downloaded response bytes.</param>
    /// <param name="RemoteBinaryCandidateCount">The total candidate binaries inspected.</param>
    /// <param name="RemoteP5ValidationAttemptCount">The total unchanged P5 attempts.</param>
    /// <param name="RemoteExactCount">The total exact remote materials accepted.</param>
    /// <param name="UnavailableCount">The total references left unavailable.</param>
    /// <param name="LimitHitCount">The total hard-limit stops.</param>
    /// <param name="CacheHits">The total positive and negative cache hits.</param>
    /// <param name="DurationTicks">The total acquisition duration in stopwatch ticks.</param>
    /// <param name="Providers">The immutable provider-level snapshots.</param>
    internal sealed record ExternalRemoteReferenceAcquisitionStatistics(
        long RemoteSearchCount,
        long RemoteRequestCount,
        long RemoteArtifactRequestCount,
        long RemoteDownloadedBytes,
        long RemoteBinaryCandidateCount,
        long RemoteP5ValidationAttemptCount,
        long RemoteExactCount,
        long UnavailableCount,
        long LimitHitCount,
        long CacheHits,
        long DurationTicks,
        ImmutableArray<ExternalRemoteArtifactProviderStatistics> Providers);
}
