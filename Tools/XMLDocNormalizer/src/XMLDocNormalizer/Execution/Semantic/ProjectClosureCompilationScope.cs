using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Groups one analysis compilation with the source types declared by its
    /// assembly.
    /// </summary>
    /// <param name="Compilation">
    /// The compilation represented by the scope.
    /// </param>
    /// <param name="SourceTypes">
    /// The named source types declared by the compilation, including nested
    /// types.
    /// </param>
    internal sealed record ProjectClosureCompilationScope(
        Compilation Compilation,
        IReadOnlyList<INamedTypeSymbol> SourceTypes);
}
