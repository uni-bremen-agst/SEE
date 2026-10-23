using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Evaluates completed exception-flow summary graphs.
    /// </summary>
    /// <remarks>
    /// The evaluator has no semantic-environment or graph-construction
    /// dependency. All mutable traversal state is local to one evaluation.
    /// </remarks>
    internal sealed class ExceptionFlowSummaryGraphEvaluator
    {
        /// <summary>
        /// Evaluates one root of a completely constructed summary graph
        /// without recursive CLR method calls.
        /// </summary>
        /// <remarks>
        /// Completed results are reused for the same node and the same
        /// excluded ancestors within its strongly connected component.
        /// Acyclic nodes retain node-only reuse. Recursive intermediate
        /// results are never published as unrestricted node summaries.
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
        internal ExceptionFlowAnalysisResult Evaluate(
                ExceptionFlowSummaryGraph graph,
                ExceptionFlowCallableKey rootKey,
                Compilation rootCompilation)
        {
            Dictionary<
                (ExceptionFlowCallableKey Key, string ExcludedMembers),
                SummaryEvaluationFrame> completedFrames =
                    new();

            Dictionary<ExceptionFlowCallableKey, (int Component, int Member)>
                components = FindSummaryComponents(graph, rootKey);

            Dictionary<int, SortedSet<int>> activeComponentMembers = new();

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

                    var membership = components[frame.Key];
                    if (!activeComponentMembers.TryGetValue(membership.Component, out var members))
                    {
                        members = new SortedSet<int>();
                        activeComponentMembers.Add(membership.Component, members);
                    }
                    members.Add(membership.Member);

                    if (frame.Summary == null ||
                        !frame.Summary.HasExecutableBody)
                    {
                        CompleteSummaryEvaluationFrame(
                            frames,
                            activeKeys,
                            completedFrames,
                            components,
                            activeComponentMembers,
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
                        components,
                        activeComponentMembers,
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
                    // Cycle-erased path exclusion, not a read of an active
                    // frame's provisional result. Every dependent cache key
                    // retains the relevant ancestor exclusions below.
                    continue;
                }

                int targetComponent = components[edge.Target].Component;
                string excludedMembers = activeComponentMembers.TryGetValue(targetComponent, out var excluded)
                    ? string.Join(",", excluded)
                    : string.Empty;

                if (completedFrames.TryGetValue(
                        (edge.Target, excludedMembers),
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
                        edge,
                        excludedMembers));
            }

            if (completedFrames.TryGetValue(
                    (rootKey, string.Empty),
                    out SummaryEvaluationFrame? rootFrame) &&
                rootFrame != null)
            {
                return rootFrame.Result;
            }

            return new ExceptionFlowAnalysisResult();
        }

        /// <summary>
        /// Finds reachable strongly connected components using an explicit
        /// Tarjan stack. IDs follow edge-list discovery, never hash iteration.
        /// </summary>
        /// <param name="graph">The frozen summary graph.</param>
        /// <param name="rootKey">The selected root.</param>
        /// <returns>Component and unique member IDs for each reachable key.</returns>
        private static Dictionary<ExceptionFlowCallableKey, (int Component, int Member)> FindSummaryComponents(
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowCallableKey rootKey)
        {
            Dictionary<ExceptionFlowCallableKey, int> indices = new();
            Dictionary<ExceptionFlowCallableKey, int> lowlinks = new();
            Dictionary<ExceptionFlowCallableKey, (int Component, int Member)> components = new();
            HashSet<ExceptionFlowCallableKey> pending = new();
            Stack<ExceptionFlowCallableKey> members = new();
            Stack<SummaryEvaluationFrame> traversal = new();
            int nextComponent = 0;
            traversal.Push(new SummaryEvaluationFrame(rootKey, incomingEdge: null));
            while (traversal.Count > 0)
            {
                SummaryEvaluationFrame frame = traversal.Peek();
                if (!frame.IsEntered)
                {
                    frame.IsEntered = true;
                    indices.Add(frame.Key, indices.Count);
                    lowlinks.Add(frame.Key, indices[frame.Key]);
                    pending.Add(frame.Key);
                    members.Push(frame.Key);
                    graph.TryGetSummary(frame.Key, out var summary);
                    frame.Summary = summary;
                }

                if (frame.Summary is { HasExecutableBody: true } body && frame.NextEdgeIndex < body.CallEdges.Count)
                {
                    ExceptionFlowCallableKey target = body.CallEdges[frame.NextEdgeIndex++].Target;
                    if (!indices.ContainsKey(target))
                    {
                        traversal.Push(new SummaryEvaluationFrame(target, incomingEdge: null));
                    }
                    else if (pending.Contains(target))
                    {
                        lowlinks[frame.Key] = Math.Min(lowlinks[frame.Key], indices[target]);
                    }
                    continue;
                }

                traversal.Pop();
                if (lowlinks[frame.Key] == indices[frame.Key])
                {
                    ExceptionFlowCallableKey member;
                    do
                    {
                        member = members.Pop();
                        pending.Remove(member);
                        components.Add(member, (nextComponent, indices[member]));
                    }
                    while (member != frame.Key);
                    nextComponent++;
                }
                if (traversal.TryPeek(out var parent))
                {
                    lowlinks[parent.Key] = Math.Min(lowlinks[parent.Key], lowlinks[frame.Key]);
                }
            }
            return components;
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
                ExceptionFlowUncertaintyRecorder.AddSymbol(
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
                ExceptionFlowUncertaintyRecorder.AddSymbol(
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
        /// <param name="components">The component membership of each key.</param>
        /// <param name="activeComponentMembers">The current path's excluded component members.</param>
        /// <param name="rootCompilation">
        /// The compilation used to normalize caught exception types.
        /// </param>
        private static void CompleteSummaryEvaluationFrame(
            Stack<SummaryEvaluationFrame> frames,
            HashSet<ExceptionFlowCallableKey> activeKeys,
            Dictionary<
                (ExceptionFlowCallableKey Key, string ExcludedMembers),
                SummaryEvaluationFrame> completedFrames,
            Dictionary<ExceptionFlowCallableKey, (int Component, int Member)> components,
            Dictionary<int, SortedSet<int>> activeComponentMembers,
            Compilation rootCompilation)
        {
            SummaryEvaluationFrame completedFrame =
                frames.Pop();

            activeKeys.Remove(
                completedFrame.Key);

            var membership = components[completedFrame.Key];
            activeComponentMembers[membership.Component].Remove(membership.Member);

            completedFrames[
                (completedFrame.Key, completedFrame.ExcludedMembers)] =
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
            // Raw diagnostic evaluation may retain its source-owned symbol.
            // Session.Analyze separately requires every published concrete
            // result to round-trip uniquely in the destination compilation.
            return new Canonical.RoslynCanonicalIdentityResolver(rootCompilation).ResolveType(
                Canonical.RoslynCanonicalIdentityFactory.CreateTypeIdentity(exceptionType)) as INamedTypeSymbol ?? exceptionType;
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
            /// <param name="excludedMembers">The ancestor exclusion context within this SCC.</param>
            public SummaryEvaluationFrame(
                ExceptionFlowCallableKey key,
                ExceptionFlowSummaryCallEdge? incomingEdge,
                string excludedMembers = "")
            {
                Key =
                    key;

                IncomingEdge =
                    incomingEdge;

                ExcludedMembers = excludedMembers;
            }

            /// <summary>Gets the closed result's required ancestor exclusions.</summary>
            /// <value>The sorted component-member IDs, or empty for unrestricted reuse.</value>
            public string ExcludedMembers { get; }

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

    }
}
