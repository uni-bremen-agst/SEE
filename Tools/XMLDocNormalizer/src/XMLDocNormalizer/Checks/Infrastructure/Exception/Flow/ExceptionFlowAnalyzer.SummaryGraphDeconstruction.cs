using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for implicit Deconstruct calls and
    /// terminal conversions in assignments and deconstructing foreach
    /// variables.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects compiler-selected <c>Deconstruct</c> methods and
        /// user-defined terminal conversions in the current callable.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for deconstruction binding information.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving compiler-selected callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryDeconstructions(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (AssignmentExpressionSyntax assignment
                     in GetSummaryDescendantsAndSelf
                         <AssignmentExpressionSyntax>(node))
            {
                if (!IsSummaryDeconstructionAssignment(
                        assignment))
                {
                    continue;
                }

                DeconstructionInfo deconstructionInfo =
                    semanticModel.GetDeconstructionInfo(
                        assignment);

                TypeInfo receiverTypeInfo =
                    semanticModel.GetTypeInfo(
                        assignment.Right);

                ITypeSymbol? receiverType =
                    receiverTypeInfo.Type ??
                    receiverTypeInfo.ConvertedType;

                AnalyzeSummaryDeconstructionTree(
                    deconstructionInfo,
                    assignment,
                    assignment.Right,
                    receiverType,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            foreach (ForEachVariableStatementSyntax forEachStatement
                     in GetSummaryDescendantsAndSelf
                         <ForEachVariableStatementSyntax>(node))
            {
                DeconstructionInfo deconstructionInfo =
                    semanticModel.GetDeconstructionInfo(
                        forEachStatement);

                ForEachStatementInfo forEachInfo =
                    semanticModel.GetForEachStatementInfo(
                        forEachStatement);

                AnalyzeSummaryDeconstructionTree(
                    deconstructionInfo,
                    forEachStatement,
                    receiverExpression: null,
                    forEachInfo.ElementType,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Traverses one Roslyn deconstruction-information tree and records
        /// its selected <c>Deconstruct</c> methods and terminal user-defined
        /// conversions.
        /// </summary>
        /// <param name="deconstructionInfo">
        /// The current deconstruction-information node.
        /// </param>
        /// <param name="sourceNode">
        /// The source assignment or foreach statement.
        /// </param>
        /// <param name="receiverExpression">
        /// The source expression whose value receives the top-level
        /// <c>Deconstruct</c> call, or <see langword="null"/> for nested or
        /// compiler-generated receivers.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type supplied by the source construct, or
        /// <see langword="null"/> when only the selected method exposes the
        /// receiver type.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver value facts.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryDeconstructionTree(
            DeconstructionInfo deconstructionInfo,
            SyntaxNode sourceNode,
            ExpressionSyntax? receiverExpression,
            ITypeSymbol? receiverType,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (deconstructionInfo.Method
                is IMethodSymbol deconstructMethod)
            {
                AddSummaryDeconstructionMethodEdges(
                    deconstructMethod,
                    sourceNode,
                    receiverExpression,
                    receiverType,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            AddSummaryDeconstructionConversionEdge(
                deconstructionInfo,
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callContext);

            if (deconstructionInfo.Nested.IsDefaultOrEmpty)
            {
                return;
            }

            for (int nestedIndex = 0;
                 nestedIndex < deconstructionInfo.Nested.Length;
                 nestedIndex++)
            {
                DeconstructionInfo nestedInfo =
                    deconstructionInfo.Nested[nestedIndex];

                ITypeSymbol? nestedReceiverType =
                    GetSummaryNestedDeconstructionReceiverType(
                        deconstructionInfo.Method,
                        nestedIndex);

                AnalyzeSummaryDeconstructionTree(
                    nestedInfo,
                    sourceNode,
                    receiverExpression: null,
                    nestedReceiverType,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Gets the static receiver type of one nested deconstruction from the
        /// corresponding output parameter of its containing
        /// <c>Deconstruct</c> method.
        /// </summary>
        /// <param name="selectedMethod">
        /// The containing <c>Deconstruct</c> method, or
        /// <see langword="null"/> for tuple deconstruction.
        /// </param>
        /// <param name="nestedIndex">
        /// The zero-based position in Roslyn's nested deconstruction array.
        /// </param>
        /// <returns>
        /// The corresponding output-parameter type, or
        /// <see langword="null"/> when no parameter can be mapped.
        /// </returns>
        private static ITypeSymbol?
            GetSummaryNestedDeconstructionReceiverType(
                IMethodSymbol? selectedMethod,
                int nestedIndex)
        {
            if (selectedMethod == null ||
                nestedIndex < 0)
            {
                return null;
            }

            int parameterOffset =
                selectedMethod.ReducedFrom == null &&
                selectedMethod.IsExtensionMethod
                    ? 1
                    : 0;

            int parameterIndex =
                parameterOffset +
                nestedIndex;

            if (parameterIndex >=
                selectedMethod.Parameters.Length)
            {
                return null;
            }

            return selectedMethod.Parameters[parameterIndex].Type;
        }

        /// <summary>
        /// Adds one direct compiler-selected <c>Deconstruct</c> call or one
        /// edge for every known compatible runtime implementation and records
        /// incomplete-target uncertainty.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method selected by deconstruction binding.
        /// </param>
        /// <param name="sourceNode">
        /// The assignment or foreach statement responsible for the call.
        /// </param>
        /// <param name="receiverExpression">
        /// The source receiver of a top-level deconstruction or reduced
        /// extension method, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type known from the source construct, including
        /// a possible type parameter, or <see langword="null"/>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver and call-context analysis.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve runtime
        /// targets.
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
        private static void AddSummaryDeconstructionMethodEdges(
            IMethodSymbol selectedMethod,
            SyntaxNode sourceNode,
            ExpressionSyntax? receiverExpression,
            ITypeSymbol? receiverType,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            IMethodSymbol targetMethod =
                selectedMethod.ReducedFrom ??
                selectedMethod;

            ExceptionFlowCallContext selectedContext =
                CreateSummaryImplicitCallContext(
                    selectedMethod,
                    targetMethod,
                    receiverExpression,
                    semanticModel,
                    callerContext);

            if (selectedMethod.ReducedFrom != null ||
                !RequiresSummaryRuntimeDispatch(
                    selectedMethod))
            {
                AddSummaryDeconstructionTargetEdge(
                    targetMethod,
                    selectedContext,
                    sourceNode,
                    graph,
                    fragment);

                return;
            }

            ITypeSymbol staticReceiverType =
                receiverType ??
                selectedMethod.ContainingType;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryDeconstructionExactReceiverType(
                    receiverExpression,
                    semanticModel);

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryRuntimeTargets(
                    selectedMethod,
                    staticReceiverType,
                    exactReceiverType,
                    semanticContext,
                    fragment);

            if (runtimeTargets.Count == 0)
            {
                AddSummaryDeconstructionTargetEdge(
                    targetMethod,
                    selectedContext,
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

                AddSummaryDeconstructionTargetEdge(
                    runtimeTarget,
                    targetContext,
                    sourceNode,
                    graph,
                    fragment);
            }
        }

        /// <summary>
        /// Gets an exact runtime receiver type from a directly created
        /// deconstructed value.
        /// </summary>
        /// <param name="receiverExpression">
        /// The deconstructed receiver expression, or
        /// <see langword="null"/> when the receiver is compiler-generated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain the receiver operation.
        /// </param>
        /// <returns>
        /// The exactly created named type, or <see langword="null"/> when no
        /// exact type is proven.
        /// </returns>
        private static INamedTypeSymbol?
            GetSummaryDeconstructionExactReceiverType(
                ExpressionSyntax? receiverExpression,
                SemanticModel semanticModel)
        {
            if (receiverExpression == null)
            {
                return null;
            }

            IOperation? receiverOperation =
                semanticModel.GetOperation(
                    receiverExpression);

            return GetSummaryExactReceiverType(
                receiverOperation);
        }

        /// <summary>
        /// Adds one resolved <c>Deconstruct</c> target edge.
        /// </summary>
        /// <param name="targetMethod">
        /// The concrete source method represented by the edge target.
        /// </param>
        /// <param name="targetContext">
        /// The call context associated with the target method.
        /// </param>
        /// <param name="sourceNode">
        /// The source assignment or foreach statement.
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
        private static void AddSummaryDeconstructionTargetEdge(
            IMethodSymbol targetMethod,
            ExceptionFlowCallContext targetContext,
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
                        ExceptionFlowPathStepKind.DeconstructCall,
                        targetMethod,
                        sourceNode)));
        }

        /// <summary>
        /// Adds the user-defined conversion associated with one terminal
        /// position in a deconstruction-information tree.
        /// </summary>
        /// <param name="deconstructionInfo">
        /// The deconstruction-information node to inspect.
        /// </param>
        /// <param name="sourceNode">
        /// The assignment or foreach statement representing the conversion.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the conversion target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the conversion edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryDeconstructionConversionEdge(
            DeconstructionInfo deconstructionInfo,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (deconstructionInfo.Conversion
                    is not Conversion conversion ||
                !conversion.IsUserDefined ||
                conversion.MethodSymbol
                    is not IMethodSymbol conversionMethod ||
                conversionMethod.MethodKind !=
                    MethodKind.Conversion)
            {
                return;
            }

            ExceptionFlowCallContext targetContext =
                CreateSummaryOperationCallContext(
                    conversionMethod,
                    [
                        null
                    ],
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    conversionMethod,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        ExceptionFlowPathStepKind
                            .ConversionOperatorCall,
                        conversionMethod,
                        sourceNode)));
        }

        /// <summary>
        /// Determines whether an assignment syntax represents deconstruction
        /// rather than an ordinary assignment.
        /// </summary>
        /// <param name="assignment">
        /// The assignment to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for tuple-shaped or declaration-shaped
        /// deconstruction assignments; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDeconstructionAssignment(
            AssignmentExpressionSyntax assignment)
        {
            if (!assignment.IsKind(
                    SyntaxKind.SimpleAssignmentExpression))
            {
                return false;
            }

            ExpressionSyntax leftExpression =
                assignment.Left;

            while (leftExpression
                   is ParenthesizedExpressionSyntax parenthesized)
            {
                leftExpression =
                    parenthesized.Expression;
            }

            if (leftExpression is TupleExpressionSyntax)
            {
                return true;
            }

            return leftExpression
                       is DeclarationExpressionSyntax declaration &&
                   declaration.Designation
                       is ParenthesizedVariableDesignationSyntax;
        }
    }
}
