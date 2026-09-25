using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Registers method-backed targets in an exception-flow summary graph.
    /// </summary>
    /// <remarks>
    /// This stateless Roslyn-bound component owns supporting-source rebinding
    /// at the graph-registration boundary. Its current dependency on
    /// <see cref="ProjectClosureSemanticContext"/> is the explicit remaining
    /// Main/P6 seam for a future historical build. It is safe for concurrent
    /// use.
    /// </remarks>
    internal static class ExceptionFlowSummaryTargetRegistrar
    {
        /// <summary>
        /// Registers a method target under its canonical supporting source
        /// symbol and transfers its call-site facts when source is available.
        /// </summary>
        /// <param name="requestedTarget">The method target selected at the call site.</param>
        /// <param name="requestedContext">The call context created for that target.</param>
        /// <param name="semanticContext">The semantic context containing supporting sources.</param>
        /// <param name="graph">The graph receiving the canonical target node.</param>
        /// <returns>The canonical graph key used by the target node and call edge.</returns>
        internal static ExceptionFlowCallableKey RegisterMethodTarget(
            IMethodSymbol requestedTarget,
            ExceptionFlowCallContext requestedContext,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph)
        {
            if (requestedTarget.DeclaringSyntaxReferences.Length == 0
                && SupportingSourceSymbolResolver.TryResolveMethod(
                    requestedTarget,
                    semanticContext,
                    out IMethodSymbol supportingSourceTarget,
                    out SemanticCompilationScope supportingSourceScope))
            {
                return RegisterMethodTarget(
                    requestedTarget,
                    requestedContext,
                    semanticContext,
                    graph,
                    supportingSourceTarget,
                    supportingSourceScope);
            }

            ExceptionFlowCallableKey requestedKey =
                new(requestedTarget, requestedContext);

            graph.GetOrAdd(requestedKey, requestedContext);
            return requestedKey;
        }

        /// <summary>
        /// Registers a method target while allowing exact external supporting
        /// source resolution through the compilation that bound the target.
        /// </summary>
        /// <param name="requestedTarget">The method target selected at the call site.</param>
        /// <param name="requestedContext">The call context created for that target.</param>
        /// <param name="semanticContext">The semantic context containing supporting sources.</param>
        /// <param name="graph">The graph receiving the canonical target node.</param>
        /// <param name="bindingCompilation">
        /// The compilation that bound <paramref name="requestedTarget"/>.
        /// </param>
        /// <returns>The canonical graph key used by the target node and call edge.</returns>
        internal static ExceptionFlowCallableKey RegisterMethodTarget(
            IMethodSymbol requestedTarget,
            ExceptionFlowCallContext requestedContext,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            Compilation bindingCompilation)
        {
            if (requestedTarget.DeclaringSyntaxReferences.Length == 0
                && SupportingSourceSymbolResolver.TryResolveMethod(
                    requestedTarget,
                    bindingCompilation,
                    semanticContext,
                    out IMethodSymbol supportingSourceTarget,
                    out SemanticCompilationScope supportingSourceScope))
            {
                return RegisterMethodTarget(
                    requestedTarget,
                    requestedContext,
                    semanticContext,
                    graph,
                    supportingSourceTarget,
                    supportingSourceScope);
            }

            return RegisterMethodTarget(
                requestedTarget,
                requestedContext,
                semanticContext,
                graph);
        }

        /// <summary>
        /// Registers a method target using a supporting source method and
        /// scope obtained from one successful resolver operation.
        /// </summary>
        /// <param name="requestedTarget">The method target selected at the call site.</param>
        /// <param name="requestedContext">The call context created for that target.</param>
        /// <param name="semanticContext">The semantic context containing supporting sources.</param>
        /// <param name="graph">The graph receiving the canonical target node.</param>
        /// <param name="supportingSourceTarget">The resolved supporting source method.</param>
        /// <param name="supportingSourceScope">
        /// The registered scope that owns <paramref name="supportingSourceTarget"/>.
        /// </param>
        /// <returns>The canonical graph key used by the target node and call edge.</returns>
        internal static ExceptionFlowCallableKey RegisterMethodTarget(
            IMethodSymbol requestedTarget,
            ExceptionFlowCallContext requestedContext,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            IMethodSymbol supportingSourceTarget,
            SemanticCompilationScope supportingSourceScope)
        {
            if (!SymbolEqualityComparer.Default.Equals(
                    supportingSourceTarget.ContainingAssembly,
                    supportingSourceScope.Compilation.Assembly))
            {
                return RegisterMethodTarget(
                    requestedTarget, requestedContext, semanticContext, graph);
            }

            ExceptionFlowCallContext supportingSourceContext = requestedContext.RebindCallable(
                supportingSourceTarget,
                memberSymbol => CrossCompilationSymbolResolver.ResolveStableMember(
                    memberSymbol,
                    supportingSourceScope.Compilation));

            ExceptionFlowCallableKey supportingSourceKey =
                new(supportingSourceTarget, supportingSourceContext);

            graph.GetOrAdd(supportingSourceKey, supportingSourceContext);
            return supportingSourceKey;
        }
    }
}
