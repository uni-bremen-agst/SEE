namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Contains the collected exception-flow paths associated with one
    /// finding.
    /// </summary>
    /// <param name="Paths">
    /// The distinct exception-flow paths.
    /// </param>
    /// <param name="PathsTruncated">
    /// Indicates whether additional paths were omitted because the
    /// configured limit was reached.
    /// </param>
    internal sealed record ExceptionFlowDetails(
        IReadOnlyList<ExceptionFlowPath> Paths,
        bool PathsTruncated);
}
