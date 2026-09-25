using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

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

            return TryResolveMethodInScope(
                methodSymbol,
                supportingScope,
                out sourceMethod,
                out scope);
        }

        /// <summary>
        /// Tries to resolve a metadata method through either an existing
        /// assembly-identical supporting source or an exact external binary
        /// registration owned by the binding compilation.
        /// </summary>
        /// <param name="methodSymbol">The metadata method to resolve.</param>
        /// <param name="bindingCompilation">
        /// The context-owned compilation that bound the method's assembly.
        /// </param>
        /// <param name="semanticContext">
        /// The semantic context containing supporting source registrations.
        /// </param>
        /// <param name="sourceMethod">The exact source-backed method.</param>
        /// <param name="scope">The scope owning the resolved method.</param>
        /// <returns>
        /// <see langword="true"/> only when the method resolves to a source
        /// declaration in an exact registered supporting compilation.
        /// </returns>
        public static bool TryResolveMethod(
            IMethodSymbol methodSymbol,
            Compilation bindingCompilation,
            ProjectClosureSemanticContext semanticContext,
            out IMethodSymbol sourceMethod,
            out SemanticCompilationScope scope)
        {
            if (methodSymbol == null)
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            if (bindingCompilation == null)
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            if (semanticContext == null)
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            IAssemblySymbol? containingAssembly = methodSymbol.ContainingAssembly;

            if (methodSymbol.DeclaringSyntaxReferences.Length != 0
                || containingAssembly == null)
            {
                sourceMethod = null!;
                scope = null!;
                return false;
            }

            bool foundScope = semanticContext.TryGetSupportingSourceScope(
                containingAssembly.Identity,
                out SemanticCompilationScope supportingScope);

            if (!foundScope)
            {
                foundScope = TryGetExternalSupportingSourceScope(
                    bindingCompilation,
                    containingAssembly,
                    semanticContext,
                    out supportingScope);
            }

            if (foundScope)
            {
                return TryResolveMethodInScope(
                    methodSymbol,
                    supportingScope,
                    out sourceMethod,
                    out scope);
            }

            sourceMethod = null!;
            scope = null!;
            return false;
        }

        /// <summary>
        /// Adapts the argument-validating P6A lookup to the fail-closed
        /// callable-resolution boundary.
        /// </summary>
        /// <param name="bindingCompilation">
        /// The context-owned compilation that bound the assembly.
        /// </param>
        /// <param name="assemblySymbol">The metadata assembly to look up.</param>
        /// <param name="semanticContext">
        /// The semantic context containing external registrations.
        /// </param>
        /// <param name="scope">The exact registered external scope.</param>
        /// <returns>
        /// <see langword="true"/> when the exact P3 binary identity is
        /// registered; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation,
            IAssemblySymbol assemblySymbol,
            ProjectClosureSemanticContext semanticContext,
            out SemanticCompilationScope scope)
        {
            if (bindingCompilation == null
                || assemblySymbol == null
                || semanticContext == null)
            {
                scope = null!;
                return false;
            }

            try
            {
                return semanticContext.TryGetExternalSupportingSourceScope(
                    bindingCompilation,
                    assemblySymbol,
                    out scope);
            }
            catch (ArgumentNullException)
            {
                scope = null!;
                return false;
            }
        }

        /// <summary>
        /// Resolves one method in an already selected exact supporting scope.
        /// </summary>
        /// <param name="methodSymbol">The metadata method to resolve.</param>
        /// <param name="supportingScope">The exact destination scope.</param>
        /// <param name="sourceMethod">The resolved source method.</param>
        /// <param name="scope">The scope owning the resolved method.</param>
        /// <returns>
        /// <see langword="true"/> when a source-backed declaration is
        /// resolved; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveMethodInScope(
            IMethodSymbol methodSymbol,
            SemanticCompilationScope supportingScope,
            out IMethodSymbol sourceMethod,
            out SemanticCompilationScope scope)
        {
            IMethodSymbol? resolvedMethod =
                ExceptionFlowCrossCompilationResolver.ResolveMethod(
                    methodSymbol,
                    supportingScope.Compilation);

            if (resolvedMethod == null
                || (resolvedMethod.DeclaringSyntaxReferences.Length == 0
                    && (!resolvedMethod.IsImplicitlyDeclared
                        || resolvedMethod.ContainingType.DeclaringSyntaxReferences.Length == 0))
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
