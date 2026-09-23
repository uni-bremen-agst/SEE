namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Captures bounded context-local source acquisition and verified
    /// line-ending reconstruction work without assigning trust to candidates.
    /// </summary>
    /// <param name="DirectExactSourceCount">Original candidates accepted by P5H.</param>
    /// <param name="ReconstructionAttemptCount">Original mismatches considered under the verified policy.</param>
    /// <param name="ReconstructionSuccessCount">Attempts producing a P5H-valid reconstructed candidate.</param>
    /// <param name="ReconstructionFailureCount">Attempts producing no P5H-valid candidate.</param>
    /// <param name="LfToCrlfSuccessCount">Successful bare-LF to CRLF candidates.</param>
    /// <param name="CrlfToLfSuccessCount">Successful CRLF to LF candidates.</param>
    /// <param name="P5HValidationAttempts">Direct and reconstructed candidate validations.</param>
    /// <param name="SourceLinkRequests">Source Link download attempts.</param>
    /// <param name="DownloadedSourceBytes">Successfully downloaded original bytes.</param>
    /// <param name="ReconstructionBytesProduced">Bytes produced across deterministic candidates.</param>
    /// <param name="ReconstructionDurationTicks">Elapsed reconstruction ticks.</param>
    /// <param name="PositiveCacheHits">Successful cached acquisition lookups.</param>
    /// <param name="NegativeCacheHits">Failed cached acquisition lookups.</param>
    internal sealed record ExternalSourceAcquisitionStatistics(
        long DirectExactSourceCount,
        long ReconstructionAttemptCount,
        long ReconstructionSuccessCount,
        long ReconstructionFailureCount,
        long LfToCrlfSuccessCount,
        long CrlfToLfSuccessCount,
        long P5HValidationAttempts,
        long SourceLinkRequests,
        long DownloadedSourceBytes,
        long ReconstructionBytesProduced,
        long ReconstructionDurationTicks,
        long PositiveCacheHits,
        long NegativeCacheHits);
}
