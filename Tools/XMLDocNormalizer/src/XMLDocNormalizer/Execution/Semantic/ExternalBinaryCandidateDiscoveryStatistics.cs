namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Captures small context-local P7A search-cost counters for tests and evaluation.
    /// </summary>
    /// <param name="ArtifactRootsExamined">The number of source roots examined.</param>
    /// <param name="DirectoriesEnumerated">The number of bounded directory enumerations.</param>
    /// <param name="CandidateFilesConsidered">The number of distinct candidate files considered.</param>
    /// <param name="CandidateFilesOpened">The number of candidate-open attempts.</param>
    /// <param name="ValidationAttempts">The number of existing P5 validation attempts.</param>
    internal sealed record ExternalBinaryCandidateDiscoveryStatistics(
        long ArtifactRootsExamined,
        long DirectoriesEnumerated,
        long CandidateFilesConsidered,
        long CandidateFilesOpened,
        long ValidationAttempts);
}
