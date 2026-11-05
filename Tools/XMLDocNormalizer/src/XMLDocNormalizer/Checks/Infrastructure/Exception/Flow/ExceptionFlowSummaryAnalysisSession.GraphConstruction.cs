using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Owns root construction and sequential pending-summary orchestration.
    /// </summary>
    internal sealed partial class ExceptionFlowSummaryAnalysisSession
    {
        /// <summary>
        /// Attempts to construct the complete context-sensitive summary graph
        /// reachable from one source-level member.
        /// </summary>
        /// <param name="member">
        /// The root member whose reachable callables should be summarized.
        /// </param>
        /// <param name="graph">
        /// The constructed summary graph.
        /// </param>
        /// <param name="rootKey">
        /// The context-sensitive key of the analyzed root member.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the root symbol and its semantic model
        /// could be resolved; otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryBuildTransitiveSummaryGraph(
            MemberDeclarationSyntax member,
            out ExceptionFlowSummaryGraph graph,
            out ExceptionFlowCallableKey? rootKey)
        {
            graph =
                new ExceptionFlowSummaryGraph();

            if (!builder.TryRegisterSummaryGraphRoot(
                    member,
                    graph,
                    out rootKey) ||
                rootKey == null)
            {
                return false;
            }

            BuildPendingSummaryNodes(
                graph);

            return true;
        }

        /// <summary>
        /// Processes every pending graph node through a nonrecursive work
        /// queue.
        /// </summary>
        /// <param name="graph">
        /// The graph containing pending callable keys.
        /// </param>
        private void BuildPendingSummaryNodes(
            ExceptionFlowSummaryGraph graph)
            => BuildPendingSummaryNodesWithProgress(graph, null);

        private static void ReportDiscoveredContexts(ExceptionFlowSummaryGraph graph, int before,
            Action<ExceptionFlowProgressEvent>? progress)
        {
            if (progress is not null && graph.Count > before)
                ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.ContextDiscovered, graph.Count - before);
        }

        private void BuildPendingSummaryNodesWithProgress(ExceptionFlowSummaryGraph graph,
            Action<ExceptionFlowProgressEvent>? progress)
        {
            while (graph.DequeuePendingOrDefault()
                   is ExceptionFlowCallableKey key)
            {
                if (!graph.TryGetSummary(
                        key,
                        out ExceptionFlowSummary? summary) ||
                    summary == null ||
                    !graph.TryGetCallContext(
                        key,
                        out ExceptionFlowCallContext? callContext) ||
                    callContext == null)
                {
                    continue;
                }

                ExceptionFlowSummaryFragment fragment =
                    new();

                ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.ContextStarted, symbol: key.Symbol);

                graph.BuildingKey = key;
                graph.BuildingContext = callContext;
                graph.BuildingCompilation = semanticContext.GetAnalysisScopes()
                    .Select(scope => scope.Compilation).FirstOrDefault(compilation =>
                        Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(compilation.Assembly, key.Symbol.ContainingAssembly)
                        || key.Symbol.DeclaringSyntaxReferences.Any(reference => compilation.ContainsSyntaxTree(reference.SyntaxTree)));

                int registeredBefore = progress is null ? 0 : graph.Count;
                bool analyzedAnyBody;
                try
                {
                    analyzedAnyBody = ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(
                        key.Symbol,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);
                }
                finally { ReportDiscoveredContexts(graph, registeredBefore, progress); }

                if (analyzedAnyBody)
                {
                    summary.MarkExecutableBodyAnalyzed();
                }

                summary.Merge(
                    fragment);

                ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.ContextProcessed, symbol: key.Symbol);

                graph.BuildingKey = null;
                graph.BuildingContext = null;
                graph.BuildingCompilation = null;
            }
        }
    }
}
