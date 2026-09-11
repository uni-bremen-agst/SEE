using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains runtime-target resolution for explicitly written virtual and
    /// interface method invocations in summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Describes how completely one invocation is covered by executable
        /// source targets.
        /// </summary>
        private enum SummaryInvocationSourceCoverage
        {
            /// <summary>No source target is available.</summary>
            NoSourceCoverage,

            /// <summary>Some source is available, but executable coverage is incomplete.</summary>
            IncompleteSourceCoverage,

            /// <summary>Every possible target has an analyzable source body.</summary>
            CompleteExecutableSourceCoverage
        }

        /// <summary>
        /// Stores one already resolved target of a planned invocation.
        /// </summary>
        private sealed class SummaryInvocationTargetPlan
        {
            /// <summary>Initializes one resolved invocation target.</summary>
            /// <param name="requestedTarget">The target used for call-context mapping.</param>
            /// <param name="analysisTarget">The canonical source target when available.</param>
            /// <param name="supportingSourceScope">
            /// The scope owning <paramref name="analysisTarget"/>, or
            /// <see langword="null"/> when no cross-compilation rebind is needed.
            /// </param>
            public SummaryInvocationTargetPlan(
                IMethodSymbol requestedTarget,
                IMethodSymbol analysisTarget,
                SemanticCompilationScope? supportingSourceScope)
            {
                RequestedTarget = requestedTarget;
                AnalysisTarget = analysisTarget;
                SupportingSourceScope = supportingSourceScope;
            }

            /// <summary>Gets the target used for call-context mapping.</summary>
            /// <value>The statically or dynamically selected target.</value>
            public IMethodSymbol RequestedTarget { get; }

            /// <summary>Gets the canonical target whose body is analyzed.</summary>
            /// <value>The source target when available; otherwise the requested target.</value>
            public IMethodSymbol AnalysisTarget { get; }

            /// <summary>Gets the scope owning a cross-compilation source target.</summary>
            /// <value>The supporting scope, or <see langword="null"/>.</value>
            public SemanticCompilationScope? SupportingSourceScope { get; }
        }

        /// <summary>
        /// Retains the source and metadata representations observed for one
        /// effective runtime target during a single dispatch traversal.
        /// </summary>
        private sealed class SummaryRuntimeTargetCandidate
        {
            /// <summary>Gets the observed metadata representation.</summary>
            /// <value>The metadata target, or <see langword="null"/>.</value>
            public IMethodSymbol? MetadataTarget { get; private set; }

            /// <summary>Gets the observed source representation.</summary>
            /// <value>The source target, or <see langword="null"/>.</value>
            public IMethodSymbol? SourceTarget { get; private set; }

            /// <summary>Adds one representation of the effective target.</summary>
            /// <param name="target">The runtime target representation.</param>
            public void AddTarget(IMethodSymbol target)
            {
                if (target.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                {
                    MetadataTarget ??= target;
                }
                else
                {
                    SourceTarget ??= target;
                }
            }
        }

        /// <summary>
        /// Stores the reusable resolution phase of one supporting-source
        /// invocation.
        /// </summary>
        private sealed class SummaryInvocationPlan
        {
            /// <summary>Initializes one invocation plan.</summary>
            /// <param name="invocation">The invocation syntax.</param>
            /// <param name="selectedMethod">The compile-time selected method.</param>
            /// <param name="semanticModel">The call-site semantic model.</param>
            /// <param name="callerContext">The caller's value facts.</param>
            /// <param name="targets">The already resolved targets.</param>
            /// <param name="isRuntimeDispatch">Whether the targets came from runtime dispatch.</param>
            /// <param name="receiverType">The static runtime-dispatch receiver type.</param>
            /// <param name="isDispatchTargetSetComplete">
            /// Whether the runtime-dispatch target set is complete.
            /// </param>
            /// <param name="sourceCoverage">The executable source coverage.</param>
            public SummaryInvocationPlan(
                InvocationExpressionSyntax invocation,
                IMethodSymbol selectedMethod,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext,
                IReadOnlyList<SummaryInvocationTargetPlan> targets,
                bool isRuntimeDispatch,
                ITypeSymbol? receiverType,
                bool isDispatchTargetSetComplete,
                SummaryInvocationSourceCoverage sourceCoverage)
            {
                Invocation = invocation;
                SelectedMethod = selectedMethod;
                SemanticModel = semanticModel;
                CallerContext = callerContext;
                Targets = targets;
                IsRuntimeDispatch = isRuntimeDispatch;
                ReceiverType = receiverType;
                IsDispatchTargetSetComplete = isDispatchTargetSetComplete;
                SourceCoverage = sourceCoverage;
            }

            /// <summary>Gets the invocation syntax.</summary>
            /// <value>The planned invocation.</value>
            public InvocationExpressionSyntax Invocation { get; }

            /// <summary>Gets the compile-time selected method.</summary>
            /// <value>The selected method.</value>
            public IMethodSymbol SelectedMethod { get; }

            /// <summary>Gets the call-site semantic model.</summary>
            /// <value>The semantic model.</value>
            public SemanticModel SemanticModel { get; }

            /// <summary>Gets the caller's value facts.</summary>
            /// <value>The caller context.</value>
            public ExceptionFlowCallContext CallerContext { get; }

            /// <summary>Gets the already resolved targets.</summary>
            /// <value>The invocation targets.</value>
            public IReadOnlyList<SummaryInvocationTargetPlan> Targets { get; }

            /// <summary>Gets whether runtime dispatch selected the targets.</summary>
            /// <value><see langword="true"/> for runtime-dispatch targets.</value>
            public bool IsRuntimeDispatch { get; }

            /// <summary>Gets the static runtime-dispatch receiver type.</summary>
            /// <value>The receiver type, or <see langword="null"/>.</value>
            public ITypeSymbol? ReceiverType { get; }

            /// <summary>Gets whether the runtime-dispatch target set is complete.</summary>
            /// <value><see langword="true"/> when no additional target can exist.</value>
            public bool IsDispatchTargetSetComplete { get; }

            /// <summary>Gets the executable source coverage.</summary>
            /// <value>The source-coverage classification.</value>
            public SummaryInvocationSourceCoverage SourceCoverage { get; }
        }

        /// <summary>
        /// Creates a reusable invocation plan for runtime dispatch or for a
        /// direct metadata call whose selected assembly has supporting source.
        /// </summary>
        /// <param name="invocation">The invocation syntax.</param>
        /// <param name="methodSymbol">The compile-time selected method.</param>
        /// <param name="semanticModel">The call-site semantic model.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="callerContext">The caller's value facts.</param>
        /// <returns>The reusable plan, or <see langword="null"/>.</returns>
        private static SummaryInvocationPlan? TryCreateSupportingSourceInvocationPlan(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowCallContext callerContext)
        {
            if (semanticModel.GetOperation(invocation)
                    is IInvocationOperation invocationOperation
                && invocationOperation.IsVirtual)
            {
                return CreateSummaryRuntimeDispatchInvocationPlan(
                    invocation,
                    invocationOperation,
                    methodSymbol,
                    semanticModel,
                    semanticContext,
                    callerContext);
            }

            IMethodSymbol assemblyMethod = methodSymbol.ReducedFrom ?? methodSymbol;

            if (assemblyMethod.DeclaringSyntaxReferences.Length != 0
                || assemblyMethod.ContainingAssembly == null
                || !semanticContext.TryGetSupportingSourceScope(
                    assemblyMethod.ContainingAssembly.Identity,
                    out _))
            {
                return null;
            }

            SummaryInvocationTargetPlan directTarget =
                CreateSummaryDirectInvocationTargetPlan(
                    methodSymbol,
                    semanticContext);

            SummaryInvocationTargetPlan[] targets = [directTarget];

            return new SummaryInvocationPlan(
                invocation,
                methodSymbol,
                semanticModel,
                callerContext,
                targets,
                isRuntimeDispatch: false,
                receiverType: null,
                isDispatchTargetSetComplete: true,
                GetSummaryInvocationSourceCoverage(
                    targets,
                    targetSetComplete: true,
                    hasUncoveredRuntimeTarget: false,
                    semanticContext));
        }

        /// <summary>
        /// Resolves runtime targets once and creates their reusable invocation
        /// plan without mutating the graph or summary fragment.
        /// </summary>
        /// <param name="invocation">The invocation syntax.</param>
        /// <param name="invocationOperation">The Roslyn invocation operation.</param>
        /// <param name="methodSymbol">The compile-time selected method.</param>
        /// <param name="semanticModel">The call-site semantic model.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="callerContext">The caller's value facts.</param>
        /// <returns>The completed runtime-dispatch plan.</returns>
        private static SummaryInvocationPlan CreateSummaryRuntimeDispatchInvocationPlan(
            InvocationExpressionSyntax invocation,
            IInvocationOperation invocationOperation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowCallContext callerContext)
        {
            ITypeSymbol? receiverType = invocationOperation.Instance?.Type;
            INamedTypeSymbol? exactReceiverType =
                GetSummaryExactReceiverType(invocationOperation.Instance);
            IReadOnlyList<SummaryRuntimeTargetCandidate> runtimeTargets =
                ResolveSummaryRuntimeTargetCandidates(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext);
            bool targetSetComplete = IsSummaryDispatchTargetSetComplete(
                methodSymbol,
                receiverType,
                exactReceiverType,
                semanticContext);
            List<SummaryInvocationTargetPlan> resolvedTargets = new();
            bool hasUncoveredRuntimeTarget = false;

            foreach (SummaryRuntimeTargetCandidate runtimeTarget in runtimeTargets)
            {
                if (runtimeTarget.MetadataTarget != null
                    && SupportingSourceSymbolResolver.TryResolveMethod(
                        runtimeTarget.MetadataTarget,
                        semanticContext,
                        out IMethodSymbol supportingSourceTarget,
                        out SemanticCompilationScope supportingSourceScope))
                {
                    resolvedTargets.Add(
                        new SummaryInvocationTargetPlan(
                            runtimeTarget.MetadataTarget,
                            supportingSourceTarget,
                            supportingSourceScope));
                }
                else if (runtimeTarget.SourceTarget != null)
                {
                    resolvedTargets.Add(
                        new SummaryInvocationTargetPlan(
                            runtimeTarget.SourceTarget,
                            runtimeTarget.SourceTarget,
                            supportingSourceScope: null));
                }
                else
                {
                    hasUncoveredRuntimeTarget = true;
                }
            }

            SummaryInvocationTargetPlan[] targets;
            bool isRuntimeDispatch;

            if (resolvedTargets.Count == 0)
            {
                targets =
                [
                    CreateSummaryDirectInvocationTargetPlan(
                        methodSymbol,
                        semanticContext)
                ];
                isRuntimeDispatch = false;
            }
            else
            {
                targets = resolvedTargets
                    .OrderBy(
                        static target => target.AnalysisTarget.ToDisplayString(
                            SymbolDisplayFormat.CSharpErrorMessageFormat),
                        StringComparer.Ordinal)
                    .ToArray();
                isRuntimeDispatch = true;
            }

            return new SummaryInvocationPlan(
                invocation,
                methodSymbol,
                semanticModel,
                callerContext,
                targets,
                isRuntimeDispatch,
                receiverType,
                targetSetComplete,
                GetSummaryInvocationSourceCoverage(
                    targets,
                    targetSetComplete,
                    hasUncoveredRuntimeTarget,
                    semanticContext));
        }

        /// <summary>
        /// Resolves the direct analysis target once, retaining the supporting
        /// scope needed for canonical graph registration.
        /// </summary>
        /// <param name="methodSymbol">The compile-time selected method.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>The resolved direct target plan.</returns>
        private static SummaryInvocationTargetPlan CreateSummaryDirectInvocationTargetPlan(
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            IMethodSymbol requestedTarget = GetSummaryInvocationAnalysisTarget(
                methodSymbol,
                semanticContext,
                out IMethodSymbol? supportingSourceTarget,
                out SemanticCompilationScope? supportingSourceScope);

            if (supportingSourceTarget == null
                && requestedTarget.DeclaringSyntaxReferences.Length == 0
                && SupportingSourceSymbolResolver.TryResolveMethod(
                    requestedTarget,
                    semanticContext,
                    out IMethodSymbol resolvedSourceTarget,
                    out SemanticCompilationScope resolvedSourceScope))
            {
                supportingSourceTarget = resolvedSourceTarget;
                supportingSourceScope = resolvedSourceScope;
            }

            return new SummaryInvocationTargetPlan(
                requestedTarget,
                supportingSourceTarget ?? requestedTarget,
                supportingSourceScope);
        }

        /// <summary>
        /// Classifies executable source coverage over an already resolved
        /// invocation target set.
        /// </summary>
        /// <param name="targets">The resolved invocation targets.</param>
        /// <param name="targetSetComplete">Whether runtime dispatch is complete.</param>
        /// <param name="hasUncoveredRuntimeTarget">
        /// Whether at least one effective runtime target lacks source.
        /// </param>
        /// <param name="semanticContext">The semantic context used for body analysis.</param>
        /// <returns>The source-coverage classification.</returns>
        private static SummaryInvocationSourceCoverage GetSummaryInvocationSourceCoverage(
            IReadOnlyList<SummaryInvocationTargetPlan> targets,
            bool targetSetComplete,
            bool hasUncoveredRuntimeTarget,
            ProjectClosureSemanticContext semanticContext)
        {
            bool hasSourceTarget = false;
            bool allTargetsHaveExecutableSource = targets.Count > 0;

            foreach (SummaryInvocationTargetPlan target in targets)
            {
                bool hasSource =
                    target.AnalysisTarget.DeclaringSyntaxReferences.Length > 0;
                hasSourceTarget |= hasSource;
                allTargetsHaveExecutableSource &=
                    hasSource
                    && HasAnalyzableSummaryInvocationBody(
                        target.AnalysisTarget,
                        semanticContext);
            }

            if (targetSetComplete
                && !hasUncoveredRuntimeTarget
                && allTargetsHaveExecutableSource)
            {
                return SummaryInvocationSourceCoverage
                    .CompleteExecutableSourceCoverage;
            }

            return hasSourceTarget
                ? SummaryInvocationSourceCoverage.IncompleteSourceCoverage
                : SummaryInvocationSourceCoverage.NoSourceCoverage;
        }

        /// <summary>
        /// Emits edges and dispatch uncertainty from one already resolved
        /// invocation plan.
        /// </summary>
        /// <param name="plan">The reusable invocation plan.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="graph">The graph receiving target summaries.</param>
        /// <param name="fragment">The fragment receiving edges and uncertainty.</param>
        private static void AddSummaryInvocationEdges(
            SummaryInvocationPlan plan,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
            if (!plan.IsDispatchTargetSetComplete)
            {
                fragment.AddUncertainTarget(
                    CreateSummaryDispatchUncertainty(
                        plan.SelectedMethod,
                        plan.ReceiverType));
            }

            ExceptionFlowPathStepKind dispatchStepKind =
                plan.SelectedMethod.ContainingType.TypeKind == TypeKind.Interface
                    ? ExceptionFlowPathStepKind.InterfaceMethodCall
                    : ExceptionFlowPathStepKind.VirtualMethodCall;

            foreach (SummaryInvocationTargetPlan target in plan.Targets)
            {
                ExceptionFlowCallContext targetContext = plan.IsRuntimeDispatch
                    ? CreateDispatchCallContext(
                        plan.SelectedMethod,
                        target.RequestedTarget,
                        plan.Invocation.ArgumentList.Arguments,
                        plan.SemanticModel,
                        plan.CallerContext)
                    : CreateInvocationCallContext(
                        plan.Invocation,
                        plan.SelectedMethod,
                        target.RequestedTarget,
                        plan.SemanticModel,
                        plan.CallerContext);
                ExceptionFlowCallableKey targetKey =
                    target.SupportingSourceScope != null
                        ? RegisterSummaryMethodTarget(
                            target.RequestedTarget,
                            targetContext,
                            semanticContext,
                            graph,
                            target.AnalysisTarget,
                            target.SupportingSourceScope)
                        : RegisterSummaryMethodTarget(
                            target.AnalysisTarget,
                            targetContext,
                            semanticContext,
                            graph);
                ExceptionFlowPathStepKind stepKind = plan.IsRuntimeDispatch
                    ? dispatchStepKind
                    : target.RequestedTarget.MethodKind == MethodKind.LocalFunction
                        ? ExceptionFlowPathStepKind.LocalFunctionCall
                        : ExceptionFlowPathStepKind.MethodCall;

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            stepKind,
                            target.RequestedTarget,
                            plan.Invocation)));
            }
        }

        /// <summary>
        /// Adds one direct invocation edge or one edge for every known runtime
        /// target of a virtual invocation.
        /// </summary>
        /// <param name="invocation">
        /// The invocation syntax being analyzed.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to locate runtime target
        /// declarations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving target summaries.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryInvocationEdges(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (semanticModel.GetOperation(invocation)
                    is not IInvocationOperation invocationOperation ||
                !invocationOperation.IsVirtual)
            {
                AddSummaryDirectInvocationEdge(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryInvocationRuntimeTargets(
                    invocationOperation,
                    methodSymbol,
                    semanticContext,
                    fragment);

            if (runtimeTargets.Count == 0)
            {
                AddSummaryDirectInvocationEdge(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            ExceptionFlowPathStepKind stepKind =
                methodSymbol.ContainingType.TypeKind ==
                    TypeKind.Interface
                        ? ExceptionFlowPathStepKind
                            .InterfaceMethodCall
                        : ExceptionFlowPathStepKind
                            .VirtualMethodCall;

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchCallContext(
                        methodSymbol,
                        runtimeTarget,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext);

                ExceptionFlowCallableKey targetKey = RegisterSummaryMethodTarget(
                    runtimeTarget, targetContext, semanticContext, graph);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            stepKind,
                            runtimeTarget,
                            invocation)));
            }
        }

        /// <summary>
        /// Adds one direct invocation edge to the summary graph.
        /// </summary>
        /// <param name="invocation">
        /// The invocation syntax being analyzed.
        /// </param>
        /// <param name="methodSymbol">
        /// The statically selected target method.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target summary.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryDirectInvocationEdge(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            IMethodSymbol targetMethod =
                GetSummaryInvocationAnalysisTarget(
                    methodSymbol,
                    semanticContext,
                    out IMethodSymbol? resolvedSupportingSourceTarget,
                    out SemanticCompilationScope? resolvedSupportingSourceScope);

            ExceptionFlowCallContext targetContext =
                CreateInvocationCallContext(
                    invocation,
                    methodSymbol,
                    targetMethod,
                    semanticModel,
                    callerContext);

            ExceptionFlowCallableKey targetKey =
                resolvedSupportingSourceTarget != null
                && resolvedSupportingSourceScope != null
                    ? RegisterSummaryMethodTarget(
                        targetMethod,
                        targetContext,
                        semanticContext,
                        graph,
                        resolvedSupportingSourceTarget,
                        resolvedSupportingSourceScope)
                    : RegisterSummaryMethodTarget(
                        targetMethod, targetContext, semanticContext, graph);

            ExceptionFlowPathStepKind stepKind =
                targetMethod.MethodKind ==
                    MethodKind.LocalFunction
                        ? ExceptionFlowPathStepKind
                            .LocalFunctionCall
                        : ExceptionFlowPathStepKind
                            .MethodCall;

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        targetMethod,
                        invocation)));
        }

        /// <summary>
        /// Resolves every known executable runtime target for one virtual or
        /// interface invocation and records incomplete-target uncertainty.
        /// </summary>
        /// <param name="invocationOperation">
        /// The Roslyn invocation operation.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="semanticContext">
        /// The semantic context containing analysis compilations and source
        /// types.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving dispatch uncertainty.
        /// </param>
        /// <returns>
        /// The distinct known runtime target methods compatible with the
        /// static receiver.
        /// </returns>
        private static IReadOnlyList<IMethodSymbol>
            ResolveSummaryInvocationRuntimeTargets(
                IInvocationOperation invocationOperation,
                IMethodSymbol methodSymbol,
                ProjectClosureSemanticContext semanticContext,
                ExceptionFlowSummaryFragment fragment)
        {
            ITypeSymbol? receiverType =
                invocationOperation.Instance?.Type;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryExactReceiverType(
                    invocationOperation.Instance);

            return ResolveSummaryRuntimeTargets(
                methodSymbol,
                receiverType,
                exactReceiverType,
                semanticContext,
                fragment);
        }

        /// <summary>
        /// Resolves every known executable runtime target for one virtual or
        /// interface member while restricting candidates to the static
        /// receiver type or generic receiver constraints.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method or accessor selected by compile-time binding.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type, including a possible type parameter, or
        /// <see langword="null"/>.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type when proven directly from the
        /// source, or <see langword="null"/>.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// The distinct known executable runtime targets.
        /// </returns>
        private static IReadOnlyList<IMethodSymbol>
            ResolveSummaryRuntimeTargets(
                IMethodSymbol methodSymbol,
                ITypeSymbol? receiverType,
                INamedTypeSymbol? exactReceiverType,
                ProjectClosureSemanticContext semanticContext)
        {
            return ResolveSummaryRuntimeTargetCandidates(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext)
                .Where(static target => target.SourceTarget != null)
                .Select(static target => target.SourceTarget!)
                .OrderBy(
                    static target => target.ToDisplayString(
                        SymbolDisplayFormat.CSharpErrorMessageFormat),
                    StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Resolves every distinct effective runtime target, retaining both
        /// source and metadata representations for coverage planning.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method or accessor selected by compile-time binding.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type, including a possible type parameter, or
        /// <see langword="null"/>.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type when proven directly from source,
        /// or <see langword="null"/>.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// The distinct effective targets observed in one runtime-type
        /// traversal.
        /// </returns>
        private static IReadOnlyList<SummaryRuntimeTargetCandidate>
            ResolveSummaryRuntimeTargetCandidates(
                IMethodSymbol methodSymbol,
                ITypeSymbol? receiverType,
                INamedTypeSymbol? exactReceiverType,
                ProjectClosureSemanticContext semanticContext)
        {
            Dictionary<string, SummaryRuntimeTargetCandidate> runtimeTargets =
                new(StringComparer.Ordinal);

            foreach (SemanticCompilationScope scope
                     in semanticContext.GetAnalysisCompilationScopes())
            {
                IMethodSymbol? scopedMethod =
                    CrossCompilationSymbolResolver.ResolveMethod(
                        methodSymbol,
                        scope.Compilation);

                if (scopedMethod == null)
                {
                    continue;
                }

                if (exactReceiverType != null)
                {
                    INamedTypeSymbol? scopedExactReceiverType =
                        CrossCompilationSymbolResolver.ResolveNamedType(
                            exactReceiverType,
                            scope.Compilation);

                    if (scopedExactReceiverType != null &&
                        IsSummaryCompatibleRuntimeReceiver(
                            scopedExactReceiverType,
                            receiverType,
                            scope.Compilation))
                    {
                        TryAddSummaryRuntimeTarget(
                            scopedExactReceiverType,
                            scopedMethod,
                            runtimeTargets);
                    }

                    continue;
                }

                foreach (INamedTypeSymbol candidateType
                         in scope.SourceTypes)
                {
                    if (!IsSummaryCompatibleRuntimeReceiver(
                            candidateType,
                            receiverType,
                            scope.Compilation))
                    {
                        continue;
                    }

                    TryAddSummaryRuntimeTarget(
                        candidateType,
                        scopedMethod,
                        runtimeTargets);
                }
            }

            return runtimeTargets
                .OrderBy(static target => target.Key, StringComparer.Ordinal)
                .Select(static target => target.Value)
                .ToArray();
        }

        /// <summary>
        /// Resolves and adds the effective method reached for one possible
        /// runtime receiver type without discarding metadata-only targets.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete runtime receiver type.
        /// </param>
        /// <param name="methodSymbol">
        /// The virtual class member or interface member selected by static
        /// binding.
        /// </param>
        /// <param name="runtimeTargets">
        /// The destination containing distinct effective runtime targets.
        /// </param>
        private static void TryAddSummaryRuntimeTarget(
            INamedTypeSymbol runtimeType,
            IMethodSymbol methodSymbol,
            Dictionary<string, SummaryRuntimeTargetCandidate> runtimeTargets)
        {
            if (runtimeType.IsAbstract ||
                runtimeType.TypeKind ==
                TypeKind.Interface)
            {
                return;
            }

            IMethodSymbol? runtimeTarget;

            if (methodSymbol.ContainingType.TypeKind ==
                TypeKind.Interface)
            {
                runtimeTarget =
                    runtimeType.FindImplementationForInterfaceMember(
                        methodSymbol)
                    as IMethodSymbol;
            }
            else
            {
                runtimeTarget =
                    ResolveSummaryMostDerivedRuntimeOverride(
                        runtimeType,
                        methodSymbol);
            }

            if (runtimeTarget == null)
            {
                return;
            }

            runtimeTarget =
                ResolveSummaryMostDerivedRuntimeOverride(
                    runtimeType,
                    runtimeTarget);

            if (runtimeTarget.IsAbstract)
            {
                return;
            }

            string targetKey =
                CreateSummaryRuntimeTargetKey(
                    runtimeTarget);

            if (!runtimeTargets.TryGetValue(
                    targetKey,
                    out SummaryRuntimeTargetCandidate? targetCandidate))
            {
                targetCandidate = new SummaryRuntimeTargetCandidate();
                runtimeTargets.Add(targetKey, targetCandidate);
            }

            targetCandidate.AddTarget(runtimeTarget);
        }

        /// <summary>
        /// Resolves the most-derived override of an implementation method for
        /// one possible runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The concrete receiver type whose executed implementation should be
        /// determined.
        /// </param>
        /// <param name="implementationMethod">
        /// The class method initially selected directly or as the
        /// implementation of an interface member.
        /// </param>
        /// <returns>
        /// The most-derived override belonging to the same virtual slot, or
        /// <paramref name="implementationMethod"/> when the method is
        /// non-virtual or no overriding method exists.
        /// </returns>
        private static IMethodSymbol
            ResolveSummaryMostDerivedRuntimeOverride(
                INamedTypeSymbol runtimeType,
                IMethodSymbol implementationMethod)
        {
            if (!implementationMethod.IsVirtual &&
                !implementationMethod.IsAbstract &&
                !implementationMethod.IsOverride)
            {
                return implementationMethod;
            }

            for (INamedTypeSymbol? currentType = runtimeType;
                 currentType != null;
                 currentType = currentType.BaseType)
            {
                foreach (IMethodSymbol candidateMethod
                         in currentType.GetMembers(
                                 implementationMethod.Name)
                             .OfType<IMethodSymbol>())
                {
                    if (!IsSummarySameVirtualMethodSlot(
                            candidateMethod,
                            implementationMethod))
                    {
                        continue;
                    }

                    return candidateMethod;
                }

                if (SymbolEqualityComparer.Default.Equals(
                        currentType.OriginalDefinition,
                        implementationMethod
                            .ContainingType
                            .OriginalDefinition))
                {
                    break;
                }
            }

            return implementationMethod;
        }

        /// <summary>
        /// Determines whether a candidate method belongs to the same virtual
        /// method slot as an initially selected implementation.
        /// </summary>
        /// <param name="candidateMethod">
        /// The possible override declared on a runtime receiver type or one of
        /// its base types.
        /// </param>
        /// <param name="implementationMethod">
        /// The implementation whose virtual slot should be matched.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the candidate is the implementation
        /// itself or overrides it directly or transitively; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummarySameVirtualMethodSlot(
            IMethodSymbol candidateMethod,
            IMethodSymbol implementationMethod)
        {
            for (IMethodSymbol? currentMethod = candidateMethod;
                 currentMethod != null;
                 currentMethod = currentMethod.OverriddenMethod)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        currentMethod.OriginalDefinition,
                        implementationMethod.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a stable deduplication key for one runtime target method.
        /// </summary>
        /// <param name="runtimeTarget">
        /// The runtime target method.
        /// </param>
        /// <returns>
        /// A key combining the containing assembly and declaration identity.
        /// </returns>
        private static string CreateSummaryRuntimeTargetKey(
            IMethodSymbol runtimeTarget)
        {
            string assemblyIdentity =
                runtimeTarget.ContainingAssembly.Identity.ToString();

            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(
                    runtimeTarget.OriginalDefinition);

            string methodIdentity =
                string.IsNullOrEmpty(
                    declarationId)
                    ? runtimeTarget.OriginalDefinition.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat)
                    : declarationId;

            return $"{assemblyIdentity}|{methodIdentity}";
        }

        /// <summary>
        /// Resolves the method that implements an interface member for one
        /// possible runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="interfaceMethod">
        /// The interface method represented in the same compilation.
        /// </param>
        /// <returns>
        /// The implicit, explicit, inherited, or default implementation, or
        /// <see langword="null"/> when the runtime type does not implement the
        /// interface member.
        /// </returns>
        private static IMethodSymbol?
            ResolveSummaryInterfaceRuntimeTarget(
                INamedTypeSymbol runtimeType,
                IMethodSymbol interfaceMethod)
        {
            INamedTypeSymbol? implementedInterface =
                FindSummaryMatchingInterface(
                    runtimeType,
                    interfaceMethod.ContainingType);

            if (implementedInterface == null)
            {
                return null;
            }

            IMethodSymbol? implementedInterfaceMethod =
                CrossCompilationSymbolResolver.ResolveMethodOnContainingType(
                    interfaceMethod,
                    implementedInterface);

            if (implementedInterfaceMethod == null)
            {
                return null;
            }

            ISymbol? implementation =
                runtimeType.FindImplementationForInterfaceMember(
                    implementedInterfaceMethod);

            if (implementation is IMethodSymbol implementationMethod)
            {
                return implementationMethod;
            }

            return implementedInterfaceMethod.IsAbstract
                ? null
                : implementedInterfaceMethod;
        }

        /// <summary>
        /// Resolves the most-derived override selected for one possible
        /// runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="virtualMethod">
        /// The virtual class method represented in the same compilation.
        /// </param>
        /// <returns>
        /// The effective override or inherited virtual method, or
        /// <see langword="null"/> when the runtime type is incompatible with
        /// the method's containing type.
        /// </returns>
        private static IMethodSymbol?
            ResolveSummaryVirtualRuntimeTarget(
                INamedTypeSymbol runtimeType,
                IMethodSymbol virtualMethod)
        {
            INamedTypeSymbol? matchingBaseType =
                FindSummaryMatchingBaseType(
                    runtimeType,
                    virtualMethod.ContainingType);

            if (matchingBaseType == null)
            {
                return null;
            }

            IMethodSymbol? matchingVirtualMethod =
                CrossCompilationSymbolResolver.ResolveMethodOnContainingType(
                    virtualMethod,
                    matchingBaseType);

            if (matchingVirtualMethod == null)
            {
                return null;
            }

            INamedTypeSymbol? currentType =
                runtimeType;

            while (currentType != null)
            {
                foreach (IMethodSymbol candidateMethod
                         in currentType.GetMembers(
                                 matchingVirtualMethod.Name)
                             .OfType<IMethodSymbol>())
                {
                    if (candidateMethod.IsStatic)
                    {
                        continue;
                    }

                    if (SymbolEqualityComparer.Default.Equals(
                            candidateMethod.OriginalDefinition,
                            matchingVirtualMethod.OriginalDefinition) ||
                        DoesSummaryMethodOverride(
                            candidateMethod,
                            matchingVirtualMethod))
                    {
                        return candidateMethod;
                    }
                }

                if (IsSummarySamePossibleType(
                        currentType,
                        matchingBaseType))
                {
                    break;
                }

                currentType = currentType.BaseType;
            }

            return matchingVirtualMethod.IsAbstract
                ? null
                : matchingVirtualMethod;
        }

        /// <summary>
        /// Determines whether one method overrides another method directly or
        /// transitively.
        /// </summary>
        /// <param name="candidateMethod">
        /// The candidate override.
        /// </param>
        /// <param name="baseMethod">
        /// The expected base method.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the candidate's override chain reaches
        /// the base method; otherwise <see langword="false"/>.
        /// </returns>
        private static bool DoesSummaryMethodOverride(
            IMethodSymbol candidateMethod,
            IMethodSymbol baseMethod)
        {
            IMethodSymbol? overriddenMethod =
                candidateMethod.OverriddenMethod;

            while (overriddenMethod != null)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        overriddenMethod.OriginalDefinition,
                        baseMethod.OriginalDefinition))
                {
                    return true;
                }

                overriddenMethod =
                    overriddenMethod.OverriddenMethod;
            }

            return false;
        }

        /// <summary>
        /// Finds the interface implemented by a runtime type that can satisfy
        /// the compile-time receiver interface.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="targetInterface">
        /// The compile-time receiver interface.
        /// </param>
        /// <returns>
        /// The matching implemented interface, or <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? FindSummaryMatchingInterface(
            INamedTypeSymbol runtimeType,
            INamedTypeSymbol targetInterface)
        {
            foreach (INamedTypeSymbol implementedInterface
                     in runtimeType.AllInterfaces)
            {
                if (IsSummarySamePossibleType(
                        implementedInterface,
                        targetInterface))
                {
                    return implementedInterface;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the base-type relation through which a runtime type can
        /// receive one virtual class method.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="targetType">
        /// The compile-time method-containing type.
        /// </param>
        /// <returns>
        /// The matching runtime or base type, or <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? FindSummaryMatchingBaseType(
            INamedTypeSymbol runtimeType,
            INamedTypeSymbol targetType)
        {
            INamedTypeSymbol? currentType =
                runtimeType;

            while (currentType != null)
            {
                if (IsSummarySamePossibleType(
                        currentType,
                        targetType))
                {
                    return currentType;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Determines whether two named types are equal or can represent the
        /// same constructed type after substituting source type parameters.
        /// </summary>
        /// <param name="candidateType">
        /// The candidate runtime relation.
        /// </param>
        /// <param name="targetType">
        /// The compile-time target type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for exact equality or compatible generic
        /// definitions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummarySamePossibleType(
            INamedTypeSymbol candidateType,
            INamedTypeSymbol targetType)
        {
            if (SymbolEqualityComparer.Default.Equals(
                    candidateType,
                    targetType))
            {
                return true;
            }

            return ContainsSummaryTypeParameter(
                       candidateType) &&
                   SymbolEqualityComparer.Default.Equals(
                       candidateType.OriginalDefinition,
                       targetType.OriginalDefinition);
        }

        /// <summary>
        /// Determines whether a type contains a source type parameter whose
        /// runtime substitution can make a generic relation compatible.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type contains a type parameter;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsSummaryTypeParameter(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol switch
            {
                ITypeParameterSymbol => true,
                IArrayTypeSymbol arrayType =>
                    ContainsSummaryTypeParameter(
                        arrayType.ElementType),
                IPointerTypeSymbol pointerType =>
                    ContainsSummaryTypeParameter(
                        pointerType.PointedAtType),
                INamedTypeSymbol namedType =>
                    namedType.TypeArguments.Any(
                        ContainsSummaryTypeParameter),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether a named type can occur as a concrete runtime
        /// receiver.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for nonabstract classes and structs;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryConcreteRuntimeType(
            INamedTypeSymbol typeSymbol)
        {
            return !typeSymbol.IsAbstract &&
                   typeSymbol.TypeKind is
                       TypeKind.Class or
                       TypeKind.Struct;
        }

        /// <summary>
        /// Extracts an exact runtime receiver type when the receiver expression
        /// directly creates the invoked object.
        /// </summary>
        /// <param name="instanceOperation">
        /// The invocation receiver operation.
        /// </param>
        /// <returns>
        /// The created named type, or <see langword="null"/> when the receiver
        /// is not known exactly.
        /// </returns>
        private static INamedTypeSymbol? GetSummaryExactReceiverType(
            IOperation? instanceOperation)
        {
            IOperation? currentOperation =
                instanceOperation;

            while (currentOperation != null)
            {
                switch (currentOperation)
                {
                    case IConversionOperation conversionOperation:
                        currentOperation =
                            conversionOperation.Operand;
                        continue;

                    case IParenthesizedOperation parenthesizedOperation:
                        currentOperation =
                            parenthesizedOperation.Operand;
                        continue;

                    case IObjectCreationOperation objectCreationOperation:
                        return objectCreationOperation.Type
                            as INamedTypeSymbol;

                    default:
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a stable cross-compilation identity for one method
        /// declaration.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method whose identity should be created.
        /// </param>
        /// <returns>
        /// The assembly-qualified documentation declaration id, or a stable
        /// display fallback when no declaration id is available.
        /// </returns>
        private static string CreateSummaryDeclarationIdentity(
            IMethodSymbol methodSymbol)
        {
            string assemblyIdentity =
                methodSymbol.ContainingAssembly?.Identity.ToString() ??
                string.Empty;

            string declarationIdentity =
                DocumentationCommentId.CreateDeclarationId(
                    methodSymbol.OriginalDefinition) ??
                methodSymbol.OriginalDefinition.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);

            return assemblyIdentity +
                   "|" +
                   declarationIdentity;
        }
    }
}
