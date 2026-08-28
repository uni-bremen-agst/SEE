using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains productive construction and evaluation of exception-flow
    /// summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Creates a reusable productive analysis session for transitive
        /// exception-flow analysis within the supplied semantic scope.
        /// </summary>
        /// <param name="semanticContext">
        /// The project-closure semantic context shared by the complete
        /// analysis run.
        /// </param>
        /// <returns>
        /// A session that reuses context-sensitive callable summaries between
        /// analyzed root members.
        /// </returns>
        internal static SummaryAnalysisSession
            CreateSummaryAnalysisSession(
                ProjectClosureSemanticContext semanticContext)
        {
            return new SummaryAnalysisSession(
                semanticContext);
        }

        /// <summary>
        /// Analyzes one member through a newly created productive
        /// summary-graph session.
        /// </summary>
        /// <param name="member">
        /// The source-level member whose escaping exception flow should be
        /// evaluated.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// The exception types, paths, truncation state, and uncertainties
        /// reachable from the member's root summary.
        /// </returns>
        public static ExceptionFlowAnalysisResult
            AnalyzeSolutionTransitivelyThrownExceptions(
                MemberDeclarationSyntax member,
                ProjectClosureSemanticContext semanticContext)
        {
            SummaryAnalysisSession session =
                CreateSummaryAnalysisSession(
                    semanticContext);

            return session.Analyze(
                member);
        }

        /// <summary>
        /// Evaluates one root of a completely constructed summary graph
        /// without recursive CLR method calls.
        /// </summary>
        /// <remarks>
        /// Each reachable context-sensitive node is evaluated at most once
        /// for the current root. Completed target results are reused when
        /// multiple call paths converge on the same graph node.
        /// </remarks>
        /// <param name="graph">
        /// The summary graph containing the root and every discovered target.
        /// </param>
        /// <param name="rootKey">
        /// The context-sensitive root node.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation whose symbols should be used by the resulting
        /// exception-flow analysis result.
        /// </param>
        /// <returns>
        /// The expanded exception-flow result.
        /// </returns>
        private static ExceptionFlowAnalysisResult
            EvaluateTransitiveSummaryGraph(
                ExceptionFlowSummaryGraph graph,
                ExceptionFlowCallableKey rootKey,
                Compilation rootCompilation)
        {
            Dictionary<
                ExceptionFlowCallableKey,
                SummaryEvaluationFrame> completedFrames =
                    new();

            HashSet<ExceptionFlowCallableKey> activeKeys =
                new();

            Stack<SummaryEvaluationFrame> frames =
                new();

            frames.Push(
                new SummaryEvaluationFrame(
                    rootKey,
                    incomingEdge: null));

            while (frames.Count > 0)
            {
                SummaryEvaluationFrame frame =
                    frames.Peek();

                if (!frame.IsEntered)
                {
                    EnterSummaryEvaluationFrame(
                        frame,
                        graph,
                        rootCompilation,
                        activeKeys);

                    if (frame.Summary == null ||
                        !frame.Summary.HasExecutableBody)
                    {
                        CompleteSummaryEvaluationFrame(
                            frames,
                            activeKeys,
                            completedFrames,
                            rootCompilation);

                        continue;
                    }
                }

                ExceptionFlowSummary summary =
                    frame.Summary!;

                if (frame.NextEdgeIndex >=
                    summary.CallEdges.Count)
                {
                    CompleteSummaryEvaluationFrame(
                        frames,
                        activeKeys,
                        completedFrames,
                        rootCompilation);

                    continue;
                }

                ExceptionFlowSummaryCallEdge edge =
                    summary.CallEdges[
                        frame.NextEdgeIndex];

                frame.NextEdgeIndex++;

                if (activeKeys.Contains(
                        edge.Target))
                {
                    continue;
                }

                if (completedFrames.TryGetValue(
                        edge.Target,
                        out SummaryEvaluationFrame?
                            completedTargetFrame) &&
                    completedTargetFrame != null)
                {
                    MergeSummaryEdgeResult(
                        frame.Result,
                        completedTargetFrame.Result,
                        edge,
                        rootCompilation);

                    continue;
                }

                frames.Push(
                    new SummaryEvaluationFrame(
                        edge.Target,
                        edge));
            }

            if (completedFrames.TryGetValue(
                    rootKey,
                    out SummaryEvaluationFrame? rootFrame) &&
                rootFrame != null)
            {
                return rootFrame.Result;
            }

            return new ExceptionFlowAnalysisResult();
        }

        /// <summary>
        /// Initializes one evaluation frame with its local exception sources,
        /// uncertainty, and executable-body state.
        /// </summary>
        /// <param name="frame">
        /// The frame to initialize.
        /// </param>
        /// <param name="graph">
        /// The summary graph containing the frame's node.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation used to normalize exception symbols.
        /// </param>
        /// <param name="activeKeys">
        /// The keys currently active on the explicit evaluation stack.
        /// </param>
        private static void EnterSummaryEvaluationFrame(
            SummaryEvaluationFrame frame,
            ExceptionFlowSummaryGraph graph,
            Compilation rootCompilation,
            HashSet<ExceptionFlowCallableKey> activeKeys)
        {
            frame.IsEntered =
                true;

            activeKeys.Add(
                frame.Key);

            if (!graph.TryGetSummary(
                    frame.Key,
                    out ExceptionFlowSummary? summary) ||
                summary == null)
            {
                MarkUncertain(
                    frame.Result,
                    frame.Key.Symbol);

                return;
            }

            frame.Summary =
                summary;

            frame.Result.UncertainTargets.UnionWith(
                summary.UncertainTargets);

            if (!summary.HasExecutableBody)
            {
                MarkUncertain(
                    frame.Result,
                    frame.Key.Symbol);

                return;
            }

            AddSummaryLocalSourcesToResult(
                summary,
                rootCompilation,
                frame.Result);
        }

        /// <summary>
        /// Adds the local exception sources of one summary without applying
        /// call-site prefixes or caller-side catch filters.
        /// </summary>
        /// <param name="summary">
        /// The summary whose local sources should be added.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation whose symbol identities should be used.
        /// </param>
        /// <param name="result">
        /// The node-local result receiving the sources.
        /// </param>
        private static void AddSummaryLocalSourcesToResult(
            ExceptionFlowSummary summary,
            Compilation rootCompilation,
            ExceptionFlowAnalysisResult result)
        {
            foreach (ExceptionFlowSummarySource source
                     in summary.Sources)
            {
                INamedTypeSymbol normalizedExceptionType =
                    NormalizeSummaryExceptionType(
                        source.ExceptionType,
                        rootCompilation);

                switch (source.Kind)
                {
                    case ExceptionFlowSourceKind.ProvenException:
                        result.AddExceptionPath(
                            normalizedExceptionType,
                            source.LocalPath);
                        break;

                    case ExceptionFlowSourceKind.ExternalDocumentationEvidence:
                        result.AddExternalDocumentationEvidencePath(
                            normalizedExceptionType,
                            source.LocalPath);
                        break;
                }
            }
        }

        /// <summary>
        /// Completes the top evaluation frame, caches its finished result, and
        /// merges that result into its caller through the incoming edge.
        /// </summary>
        /// <param name="frames">
        /// The explicit nonrecursive evaluation stack.
        /// </param>
        /// <param name="activeKeys">
        /// The callable keys active on the current graph path.
        /// </param>
        /// <param name="completedFrames">
        /// The frames already completed for the current root.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation used to normalize caught exception types.
        /// </param>
        private static void CompleteSummaryEvaluationFrame(
            Stack<SummaryEvaluationFrame> frames,
            HashSet<ExceptionFlowCallableKey> activeKeys,
            Dictionary<
                ExceptionFlowCallableKey,
                SummaryEvaluationFrame> completedFrames,
            Compilation rootCompilation)
        {
            SummaryEvaluationFrame completedFrame =
                frames.Pop();

            activeKeys.Remove(
                completedFrame.Key);

            completedFrames[
                completedFrame.Key] =
                    completedFrame;

            if (completedFrame.IncomingEdge == null ||
                frames.Count == 0)
            {
                return;
            }

            SummaryEvaluationFrame callerFrame =
                frames.Peek();

            MergeSummaryEdgeResult(
                callerFrame.Result,
                completedFrame.Result,
                completedFrame.IncomingEdge,
                rootCompilation);
        }

        /// <summary>
        /// Merges one completed target result into its caller while applying
        /// the call-site catch filter and prepending the call-site path step.
        /// </summary>
        /// <param name="callerResult">
        /// The caller result receiving the target flow.
        /// </param>
        /// <param name="targetResult">
        /// The completed target-node result.
        /// </param>
        /// <param name="edge">
        /// The call edge connecting caller and target.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation used to normalize caught exception symbols.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="targetResult"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static void MergeSummaryEdgeResult(
            ExceptionFlowAnalysisResult callerResult,
            ExceptionFlowAnalysisResult targetResult,
            ExceptionFlowSummaryCallEdge edge,
            Compilation rootCompilation)
        {
            callerResult.MergeWithPrefixExcluding(
                targetResult,
                edge.CallSiteStep,
                exceptionType => IsSummaryExceptionCaughtByEdge(exceptionType, edge, rootCompilation));
        }

        /// <summary>
        /// Determines whether one call edge's typed catch filter handles an
        /// exception escaping from its target.
        /// </summary>
        /// <param name="exceptionType">
        /// The normalized escaping exception type.
        /// </param>
        /// <param name="edge">
        /// The call edge whose catch filter should be evaluated.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation used to normalize caught exception symbols.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the edge catches the exception;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static bool IsSummaryExceptionCaughtByEdge(
            INamedTypeSymbol exceptionType,
            ExceptionFlowSummaryCallEdge edge,
            Compilation rootCompilation)
        {
            foreach (INamedTypeSymbol caughtType
                     in edge.CaughtExceptionTypes)
            {
                INamedTypeSymbol normalizedCaughtType =
                    NormalizeSummaryExceptionType(
                        caughtType,
                        rootCompilation);

                if (ExceptionFlowCaughtTypeFilter
                    .IsSameOrDerivedFrom(
                        exceptionType,
                        normalizedCaughtType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Maps an exception symbol originating in any analyzed project
        /// compilation to the corresponding symbol in the root compilation.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type collected from a local summary.
        /// </param>
        /// <param name="rootCompilation">
        /// The compilation whose symbol identity should be returned.
        /// </param>
        /// <returns>
        /// The corresponding root-compilation symbol when it can be resolved;
        /// otherwise the original exception symbol.
        /// </returns>
        private static INamedTypeSymbol NormalizeSummaryExceptionType(
            INamedTypeSymbol exceptionType,
            Compilation rootCompilation)
        {
            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(
                    exceptionType.OriginalDefinition);

            if (!string.IsNullOrWhiteSpace(
                    declarationId) &&
                DocumentationCommentId
                    .GetFirstSymbolForDeclarationId(
                        declarationId,
                        rootCompilation)
                    is INamedTypeSymbol normalizedType)
            {
                return normalizedType;
            }

            return exceptionType;
        }

        /// <summary>
        /// Stores one mutable frame of the explicit memoized summary-graph
        /// evaluation stack.
        /// </summary>
        private sealed class SummaryEvaluationFrame
        {
            /// <summary>
            /// Initializes one evaluation frame.
            /// </summary>
            /// <param name="key">
            /// The graph-node key represented by the frame.
            /// </param>
            /// <param name="incomingEdge">
            /// The edge through which the frame was reached, or
            /// <see langword="null"/> for the root.
            /// </param>
            public SummaryEvaluationFrame(
                ExceptionFlowCallableKey key,
                ExceptionFlowSummaryCallEdge? incomingEdge)
            {
                Key =
                    key;

                IncomingEdge =
                    incomingEdge;
            }

            /// <summary>
            /// Gets the graph-node key represented by the frame.
            /// </summary>
            /// <value>The context-sensitive callable key.</value>
            public ExceptionFlowCallableKey Key { get; }

            /// <summary>
            /// Gets the edge through which the node was reached.
            /// </summary>
            /// <value>
            /// The incoming call edge, or <see langword="null"/> for the root.
            /// </value>
            public ExceptionFlowSummaryCallEdge? IncomingEdge
            {
                get;
            }

            /// <summary>
            /// Gets the node-local accumulated exception-flow result.
            /// </summary>
            /// <value>The mutable node result.</value>
            public ExceptionFlowAnalysisResult Result { get; } =
                new();

            /// <summary>
            /// Gets or sets whether local sources and uncertainty were already
            /// processed.
            /// </summary>
            /// <value>The frame-entry state.</value>
            public bool IsEntered { get; set; }

            /// <summary>
            /// Gets or sets the index of the next outgoing edge to process.
            /// </summary>
            /// <value>The next outgoing edge index.</value>
            public int NextEdgeIndex { get; set; }

            /// <summary>
            /// Gets or sets the resolved callable summary.
            /// </summary>
            /// <value>
            /// The resolved summary after frame entry, or
            /// <see langword="null"/> before resolution.
            /// </value>
            public ExceptionFlowSummary? Summary { get; set; }
        }

        /// <summary>
        /// Reuses one growing context-sensitive summary graph across multiple
        /// productive transitive root analyses.
        /// </summary>
        /// <remarks>
        /// The session is intentionally not thread-safe. One instance belongs
        /// to one sequential detector or tool run.
        /// </remarks>
        internal sealed class SummaryAnalysisSession
        {
            /// <summary>
            /// The semantic scope shared by every analyzed root.
            /// </summary>
            private readonly ProjectClosureSemanticContext semanticContext;

            /// <summary>
            /// The graph reused by every root in the session.
            /// </summary>
            private readonly ExceptionFlowSummaryGraph graph =
                new();

            /// <summary>
            /// Initializes a new productive summary-graph session.
            /// </summary>
            /// <param name="semanticContext">
            /// The project-closure semantic context.
            /// </param>
            internal SummaryAnalysisSession(
                ProjectClosureSemanticContext semanticContext)
            {
                this.semanticContext =
                    semanticContext;
            }

            /// <summary>
            /// Adds one root to the shared graph, completes newly discovered
            /// summaries, and evaluates the root's escaping exception flow.
            /// </summary>
            /// <param name="member">
            /// The source-level root member.
            /// </param>
            /// <returns>
            /// The expanded exception-flow result, or an empty result when
            /// the root symbol cannot be resolved.
            /// </returns>
            public ExceptionFlowAnalysisResult Analyze(
                MemberDeclarationSyntax member)
            {
                if (!semanticContext.TryGetSemanticModel(
                        member.SyntaxTree,
                        out SemanticModel rootSemanticModel) ||
                    rootSemanticModel == null ||
                    !TryRegisterSummaryGraphRoot(
                        member,
                        semanticContext,
                        graph,
                        out ExceptionFlowCallableKey? rootKey) ||
                    rootKey == null)
                {
                    return new ExceptionFlowAnalysisResult();
                }

                BuildPendingSummaryNodes(
                    graph,
                    semanticContext);

                return EvaluateTransitiveSummaryGraph(
                    graph,
                    rootKey,
                    rootSemanticModel.Compilation);
            }
        }
    }
}
