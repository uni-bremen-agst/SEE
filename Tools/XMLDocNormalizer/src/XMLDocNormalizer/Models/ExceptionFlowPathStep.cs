namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents one source-level step in an exception-flow path.
    /// </summary>
    /// <param name="Kind">
    /// The role of the step in the exception-flow path.
    /// </param>
    /// <param name="SymbolName">
    /// The display name of the referenced symbol or exception type.
    /// </param>
    /// <param name="FilePath">
    /// The source file containing the step, if available.
    /// </param>
    /// <param name="Line">
    /// The one-based source line, if available.
    /// </param>
    /// <param name="Column">
    /// The one-based source column, if available.
    /// </param>
    internal sealed record ExceptionFlowPathStep(
        ExceptionFlowPathStepKind Kind,
        string SymbolName,
        string? FilePath,
        int? Line,
        int? Column);
}
