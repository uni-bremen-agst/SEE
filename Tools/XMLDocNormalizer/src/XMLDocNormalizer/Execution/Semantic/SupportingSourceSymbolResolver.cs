using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Resolves metadata and implicit method symbols to exact registered
    /// supporting source declarations.
    /// </summary>
    internal static class SupportingSourceSymbolResolver
    {
        /// <summary>
        /// Resolves a metadata or implicit method into the registered
        /// supporting source compilation with the same full assembly identity.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to resolve.</param>
        /// <param name="semanticContext">
        /// The semantic context containing supporting source registrations.
        /// </param>
        /// <returns>
        /// The corresponding supporting source method, or
        /// <see langword="null"/> when the input is already source-backed or
        /// no exact registered source declaration exists.
        /// </returns>
        public static IMethodSymbol? ResolveMethod(
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            return TryResolveMethod(
                    methodSymbol,
                    semanticContext,
                    out IMethodSymbol sourceMethod,
                    out _)
                ? sourceMethod
                : null;
        }

        /// <summary>
        /// Tries to resolve a metadata or implicit supporting method together
        /// with the registered scope that owns the resolved source symbol.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to resolve.</param>
        /// <param name="semanticContext">
        /// The semantic context containing supporting source registrations.
        /// </param>
        /// <param name="sourceMethod">
        /// The corresponding method from the supporting compilation when
        /// resolution succeeds.
        /// </param>
        /// <param name="scope">
        /// The registered supporting scope that owns
        /// <paramref name="sourceMethod"/> when resolution succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an assembly-identical method owned by
        /// the registered supporting compilation is resolved; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool TryResolveMethod(
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext,
            out IMethodSymbol sourceMethod,
            out SemanticCompilationScope scope)
        {
            if (methodSymbol.DeclaringSyntaxReferences.Length != 0
                || methodSymbol.ContainingAssembly == null
                || !semanticContext.TryGetSupportingSourceScope(
                    methodSymbol.ContainingAssembly.Identity,
                    out SemanticCompilationScope supportingScope))
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            IMethodSymbol? resolvedMethod =
                CrossCompilationSymbolResolver.ResolveMethod(methodSymbol, supportingScope.Compilation);

            if (resolvedMethod == null
                || !SymbolEqualityComparer.Default.Equals(
                    resolvedMethod.ContainingAssembly,
                    supportingScope.Compilation.Assembly))
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            sourceMethod = resolvedMethod;
            scope = supportingScope;
            return true;
        }
    }
}
