using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;

/// <summary>
/// Provides the active Main adapter implementation for exception-flow
/// semantic capabilities.
/// </summary>
namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Implements the active Main adapter portion of the exception-flow
    /// semantic environment.
    /// </summary>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        /// <summary>
        /// The Main-owned semantic context providing active Roslyn services.
        /// </summary>
        private readonly ProjectClosureSemanticContext semanticContext;

        /// <summary>
        /// Initializes an active exception-flow semantic environment.
        /// </summary>
        /// <param name="semanticContext">The wrapped project-closure context.</param>
        internal ExceptionFlowSemanticEnvironment(
            ProjectClosureSemanticContext semanticContext)
        {
            this.semanticContext = semanticContext;
        }

        /// <summary>
        /// Tries to obtain the semantic model owned by the active project closure.
        /// </summary>
        /// <param name="tree">The syntax tree whose model is required.</param>
        /// <param name="semanticModel">The owned semantic model when found.</param>
        /// <returns>
        /// <see langword="true"/> when the tree belongs to the active context;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryGetSemanticModel(
            SyntaxTree tree,
            out SemanticModel semanticModel)
        {
            return semanticContext.TryGetSemanticModel(tree, out semanticModel);
        }

        /// <summary>
        /// Gets every source-backed compilation in the active analysis scope.
        /// </summary>
        /// <returns>The deterministic context-owned scope snapshot.</returns>
        internal IReadOnlyList<ExceptionFlowSemanticScope> GetAnalysisScopes()
        {
            return semanticContext.GetAnalysisCompilationScopes();
        }

        /// <summary>
        /// Tries to resolve a metadata callable through registered supporting source.
        /// </summary>
        /// <param name="methodSymbol">The metadata callable to resolve.</param>
        /// <param name="resolution">The exact source resolution when found.</param>
        /// <returns>
        /// <see langword="true"/> only for exact registered source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol,
            out ExceptionFlowSupportingSourceMethod resolution)
        {
            if (!SupportingSourceSymbolResolver.TryResolveMethod(
                    methodSymbol,
                    semanticContext,
                    out IMethodSymbol sourceMethod,
                    out SemanticCompilationScope scope))
            {
                resolution = null!;
                return false;
            }

            resolution = new ExceptionFlowSupportingSourceMethod(
                sourceMethod,
                scope);
            return true;
        }

        /// <summary>
        /// Tries to resolve a metadata callable through supporting source
        /// associated with the compilation that bound it.
        /// </summary>
        /// <param name="methodSymbol">The metadata callable to resolve.</param>
        /// <param name="bindingCompilation">The compilation that bound the callable.</param>
        /// <param name="resolution">The exact source resolution when found.</param>
        /// <returns>
        /// <see langword="true"/> only for exact registered or reconstructed
        /// source; otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryResolveSupportingSourceMethod(
            IMethodSymbol methodSymbol,
            Compilation bindingCompilation,
            out ExceptionFlowSupportingSourceMethod resolution)
        {
            if (!SupportingSourceSymbolResolver.TryResolveMethod(
                    methodSymbol,
                    bindingCompilation,
                    semanticContext,
                    out IMethodSymbol sourceMethod,
                    out SemanticCompilationScope scope))
            {
                resolution = null!;
                return false;
            }

            resolution = new ExceptionFlowSupportingSourceMethod(
                sourceMethod,
                scope);
            return true;
        }

        /// <summary>
        /// Tries to obtain exact registered external supporting source for an
        /// assembly bound by the active context.
        /// </summary>
        /// <param name="bindingCompilation">The compilation that bound the assembly.</param>
        /// <param name="assemblySymbol">The bound metadata assembly.</param>
        /// <param name="scope">The exact supporting scope when found.</param>
        /// <returns>
        /// <see langword="true"/> when exact supporting source is available;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryGetExternalSupportingSourceScope(
            Compilation bindingCompilation,
            IAssemblySymbol assemblySymbol,
            out ExceptionFlowSemanticScope scope)
        {
            if (!SupportingSourceSymbolResolver.TryGetExternalSupportingSourceScope(
                    bindingCompilation,
                    assemblySymbol,
                    semanticContext,
                    out SemanticCompilationScope supportingScope))
            {
                scope = null!;
                return false;
            }

            scope = supportingScope;
            return true;
        }
    }
}
