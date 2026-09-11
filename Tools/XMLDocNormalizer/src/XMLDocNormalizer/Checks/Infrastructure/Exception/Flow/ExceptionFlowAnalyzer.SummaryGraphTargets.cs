using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains canonical registration of method-backed summary graph targets.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
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
        internal static ExceptionFlowCallableKey RegisterSummaryMethodTarget(
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
                return RegisterSummaryMethodTarget(
                    requestedTarget,
                    requestedContext,
                    semanticContext,
                    graph,
                    supportingSourceTarget,
                    supportingSourceScope);
            }

            ExceptionFlowCallableKey requestedKey =
                new(requestedTarget, requestedContext.Key);

            graph.GetOrAdd(requestedKey, requestedContext);
            return requestedKey;
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
        private static ExceptionFlowCallableKey RegisterSummaryMethodTarget(
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
                return RegisterSummaryMethodTarget(
                    requestedTarget, requestedContext, semanticContext, graph);
            }

            ExceptionFlowCallContext supportingSourceContext = requestedContext.RebindCallable(
                supportingSourceTarget,
                memberSymbol => CrossCompilationSymbolResolver.ResolveStableMember(
                    memberSymbol,
                    supportingSourceScope.Compilation));

            ExceptionFlowCallableKey supportingSourceKey =
                new(supportingSourceTarget, supportingSourceContext.Key);

            graph.GetOrAdd(supportingSourceKey, supportingSourceContext);
            return supportingSourceKey;
        }
    }
}
