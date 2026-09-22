namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Captures bounded context-local Portable PDB acquisition work.</summary>
    /// <param name="CandidatesConsidered">The number of candidate images considered.</param>
    /// <param name="CandidatesOpened">The number of file or archive-entry open attempts.</param>
    /// <param name="LocalSymbolPackagesInspected">The number of package archives inspected.</param>
    /// <param name="RemoteSymbolRequests">The number of remote requests.</param>
    /// <param name="DownloadedPdbBytes">The number of remote response bytes read.</param>
    /// <param name="ValidationAttempts">The number of existing P4B validation attempts.</param>
    /// <param name="PositiveCacheHits">The number of positive cache hits.</param>
    /// <param name="NegativeCacheHits">The number of negative cache hits.</param>
    internal sealed record ExternalPortablePdbAcquisitionStatistics(
        long CandidatesConsidered,
        long CandidatesOpened,
        long LocalSymbolPackagesInspected,
        long RemoteSymbolRequests,
        long DownloadedPdbBytes,
        long ValidationAttempts,
        long PositiveCacheHits,
        long NegativeCacheHits);
}
