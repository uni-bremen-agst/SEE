using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains runtime-target expansion for compiler-selected implicit
    /// method calls, property getters, and awaiter-pattern members.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Adds one directly selected implicit method call or one edge for
        /// every known compatible runtime implementation and records
        /// incomplete-target uncertainty.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method selected by Roslyn, or <see langword="null"/> when no
        /// method was selected.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the implicit call.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the implicit call.
        /// </param>
        /// <param name="reducedExtensionReceiver">
        /// The source expression supplied as the receiver of a reduced
        /// extension method, or <see langword="null"/>.
        /// </param>
        /// <param name="receiverType">
        /// The static instance-receiver type, including a possible type
        /// parameter, or <see langword="null"/>.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type proven directly from the source,
        /// or <see langword="null"/>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver facts and optional defaults.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving target summaries.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving call edges and uncertainty.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryImplicitDispatchMethodEdges(
            IMethodSymbol? selectedMethod,
            ExceptionFlowPathStepKind stepKind,
            SyntaxNode sourceNode,
            ExpressionSyntax? reducedExtensionReceiver,
            ITypeSymbol? receiverType,
            INamedTypeSymbol? exactReceiverType,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (selectedMethod == null)
            {
                return;
            }

            IMethodSymbol targetMethod =
                selectedMethod.ReducedFrom ??
                selectedMethod;

            ExceptionFlowCallContext selectedContext =
                CreateSummaryImplicitCallContext(
                    selectedMethod,
                    targetMethod,
                    reducedExtensionReceiver,
                    semanticModel,
                    callerContext);

            if (selectedMethod.ReducedFrom != null ||
                !RequiresSummaryRuntimeDispatch(
                    selectedMethod))
            {
                AddSummaryImplicitDispatchTargetEdge(
                    targetMethod,
                    selectedContext,
                    stepKind,
                    sourceNode,
                    graph,
                    fragment);

                return;
            }

            ITypeSymbol effectiveReceiverType =
                receiverType ??
                selectedMethod.ContainingType;

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryRuntimeTargets(
                    selectedMethod,
                    effectiveReceiverType,
                    exactReceiverType,
                    semanticContext,
                    fragment);

            if (runtimeTargets.Count == 0)
            {
                AddSummaryImplicitDispatchTargetEdge(
                    targetMethod,
                    selectedContext,
                    stepKind,
                    sourceNode,
                    graph,
                    fragment);

                return;
            }

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchTargetContext(
                        selectedMethod,
                        runtimeTarget,
                        selectedContext);

                AddSummaryImplicitDispatchTargetEdge(
                    runtimeTarget,
                    targetContext,
                    stepKind,
                    sourceNode,
                    graph,
                    fragment);
            }
        }

        /// <summary>
        /// Adds one directly selected implicit property getter or one edge for
        /// every known compatible runtime getter implementation.
        /// </summary>
        /// <param name="selectedProperty">
        /// The property selected by Roslyn, or <see langword="null"/>.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the getter.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the getter.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type, or <see langword="null"/>.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type, or <see langword="null"/>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter targets.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving getter edges.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the selected property has a getter;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            TryAddSummaryImplicitDispatchGetterEdges(
                IPropertySymbol? selectedProperty,
                ExceptionFlowPathStepKind stepKind,
                SyntaxNode sourceNode,
                ITypeSymbol? receiverType,
                INamedTypeSymbol? exactReceiverType,
                SemanticModel semanticModel,
                ProjectClosureSemanticContext semanticContext,
                ExceptionFlowSummaryGraph graph,
                ExceptionFlowSummaryFragment fragment,
                ExceptionFlowCallContext callerContext)
        {
            if (selectedProperty?.GetMethod
                is not IMethodSymbol getterMethod)
            {
                return false;
            }

            AddSummaryImplicitDispatchMethodEdges(
                getterMethod,
                stepKind,
                sourceNode,
                reducedExtensionReceiver: null,
                receiverType,
                exactReceiverType,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);

            return true;
        }

        /// <summary>
        /// Adds the complete awaiter chain selected for one explicit
        /// <c>await</c> expression, including known virtual and interface
        /// runtime implementations.
        /// </summary>
        /// <param name="awaitInfo">
        /// Roslyn's semantic information for the await operation.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the await operation.
        /// </param>
        /// <param name="awaitedExpression">
        /// The expression whose value is awaited.
        /// </param>
        /// <param name="description">
        /// A fixed description used when awaiter binding is incomplete.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving awaiter edges or uncertainty.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryExplicitAwaitDispatchEdges(
            AwaitExpressionInfo awaitInfo,
            SyntaxNode sourceNode,
            ExpressionSyntax awaitedExpression,
            string description,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (awaitInfo.IsDynamic)
            {
                fragment.AddUncertainTarget(
                    description +
                    " uses dynamic await binding.");

                return;
            }

            if (awaitInfo.RuntimeAwaitMethod != null)
            {
                AddSummaryImplicitMethodEdge(
                    awaitInfo.RuntimeAwaitMethod,
                    ExceptionFlowPathStepKind.RuntimeAwaitCall,
                    sourceNode,
                    reducedExtensionReceiver: null,
                    semanticModel,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            bool completeAwaitPattern =
                awaitInfo.GetAwaiterMethod != null &&
                awaitInfo.IsCompletedProperty?.GetMethod != null &&
                awaitInfo.GetResultMethod != null;

            if (!completeAwaitPattern)
            {
                fragment.AddUncertainTarget(
                    description +
                    " awaiter pattern could not be resolved completely.");
            }

            ITypeSymbol? awaitableReceiverType =
                GetSummaryImplicitReceiverType(
                    awaitedExpression,
                    semanticModel);

            INamedTypeSymbol? exactAwaitableReceiverType =
                GetSummaryImplicitExactReceiverType(
                    awaitedExpression,
                    semanticModel);

            AddSummaryImplicitDispatchMethodEdges(
                awaitInfo.GetAwaiterMethod,
                ExceptionFlowPathStepKind.AwaitGetAwaiterCall,
                sourceNode,
                awaitedExpression,
                awaitableReceiverType,
                exactAwaitableReceiverType,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);

            INamedTypeSymbol? awaiterType =
                awaitInfo.GetAwaiterMethod?.ReturnType
                    as INamedTypeSymbol;

            TryAddSummaryImplicitDispatchGetterEdges(
                awaitInfo.IsCompletedProperty,
                ExceptionFlowPathStepKind.AwaitIsCompletedGetter,
                sourceNode,
                awaiterType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);

            AddSummaryImplicitDispatchMethodEdges(
                awaitInfo.GetResultMethod,
                ExceptionFlowPathStepKind.AwaitGetResultCall,
                sourceNode,
                reducedExtensionReceiver: null,
                awaiterType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);
        }

        /// <summary>
        /// Resolves and adds the awaiter chain for a compiler-generated await
        /// whose awaitable exists only as the result of another implicit
        /// operation.
        /// </summary>
        /// <param name="awaitableType">
        /// The static type of the implicitly awaited value.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the implicit await.
        /// </param>
        /// <param name="description">
        /// A fixed description used when awaiter binding cannot be resolved.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative awaiter binding.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving awaiter edges or uncertainty.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryImplicitAwaitDispatchEdges(
            ITypeSymbol? awaitableType,
            SyntaxNode sourceNode,
            string description,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (awaitableType == null)
            {
                fragment.AddUncertainTarget(
                    description +
                    " awaitable type could not be resolved.");

                return;
            }

            if (awaitableType.TypeKind ==
                TypeKind.Dynamic)
            {
                fragment.AddUncertainTarget(
                    description +
                    " uses dynamic await binding.");

                return;
            }

            bool resolved =
                TryResolveSummaryAwaitableMembers(
                    awaitableType,
                    sourceNode,
                    semanticModel,
                    out IMethodSymbol? getAwaiterMethod,
                    out IPropertySymbol? isCompletedProperty,
                    out IMethodSymbol? getResultMethod);

            if (!resolved)
            {
                string typeName =
                    awaitableType.ToDisplayString(
                        SymbolDisplayFormat
                            .CSharpErrorMessageFormat);

                if (string.IsNullOrWhiteSpace(
                        typeName))
                {
                    typeName =
                        "<unknown awaitable type>";
                }

                fragment.AddUncertainTarget(
                    description +
                    $" awaiter pattern for '{typeName}' could not be resolved.");

                return;
            }

            INamedTypeSymbol? awaitableReceiverType =
                awaitableType as INamedTypeSymbol;

            AddSummaryImplicitDispatchMethodEdges(
                getAwaiterMethod,
                ExceptionFlowPathStepKind.AwaitGetAwaiterCall,
                sourceNode,
                reducedExtensionReceiver: null,
                awaitableReceiverType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);

            INamedTypeSymbol? awaiterType =
                getAwaiterMethod?.ReturnType
                    as INamedTypeSymbol;

            TryAddSummaryImplicitDispatchGetterEdges(
                isCompletedProperty,
                ExceptionFlowPathStepKind.AwaitIsCompletedGetter,
                sourceNode,
                awaiterType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);

            AddSummaryImplicitDispatchMethodEdges(
                getResultMethod,
                ExceptionFlowPathStepKind.AwaitGetResultCall,
                sourceNode,
                reducedExtensionReceiver: null,
                awaiterType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callerContext);
        }

        /// <summary>
        /// Gets the static receiver type of an implicit member call,
        /// preserving generic type parameters and constraints.
        /// </summary>
        /// <param name="receiverExpression">
        /// The source receiver expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <returns>
        /// The static or converted receiver type, or
        /// <see langword="null"/>.
        /// </returns>
        private static ITypeSymbol?
            GetSummaryImplicitReceiverType(
                ExpressionSyntax receiverExpression,
                SemanticModel semanticModel)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    receiverExpression);

            return typeInfo.Type ??
                   typeInfo.ConvertedType;
        }

        /// <summary>
        /// Gets an exact runtime receiver type from a directly created
        /// implicit-call receiver.
        /// </summary>
        /// <param name="receiverExpression">
        /// The source receiver expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain its operation.
        /// </param>
        /// <returns>
        /// The exactly created named type, or
        /// <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol?
            GetSummaryImplicitExactReceiverType(
                ExpressionSyntax receiverExpression,
                SemanticModel semanticModel)
        {
            IOperation? receiverOperation =
                semanticModel.GetOperation(
                    receiverExpression);

            return GetSummaryExactReceiverType(
                receiverOperation);
        }

        /// <summary>
        /// Adds one resolved implicit-call target edge.
        /// </summary>
        /// <param name="targetMethod">
        /// The concrete target method.
        /// </param>
        /// <param name="targetContext">
        /// The context associated with the target method.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the implicit operation.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the operation.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target summary.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving the call edge.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="targetMethod"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static void AddSummaryImplicitDispatchTargetEdge(
            IMethodSymbol targetMethod,
            ExceptionFlowCallContext targetContext,
            ExceptionFlowPathStepKind stepKind,
            SyntaxNode sourceNode,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
            ExceptionFlowCallableKey targetKey =
                new(
                    targetMethod,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        targetMethod,
                        sourceNode)));
        }
    }
}