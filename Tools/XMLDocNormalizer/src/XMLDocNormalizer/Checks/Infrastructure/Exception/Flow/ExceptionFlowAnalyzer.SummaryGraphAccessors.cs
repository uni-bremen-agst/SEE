using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for property, indexer, and event
    /// accessor operations.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects unqualified property getter accesses such as
        /// <c>Value</c> inside the containing type.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and operation resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving getter edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummarySimpleNamePropertyAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (IdentifierNameSyntax identifierName
                     in GetSummaryDescendantsAndSelf
                         <IdentifierNameSyntax>(node))
            {
                if (identifierName.Parent
                        is MemberAccessExpressionSyntax memberAccess &&
                    ReferenceEquals(
                        memberAccess.Name,
                        identifierName))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        identifierName);

                if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                IPropertyReferenceOperation? propertyOperation =
                    semanticModel.GetOperation(
                        identifierName)
                    as IPropertyReferenceOperation;

                AddSummaryPropertyGetterEdge(
                    propertySymbol,
                    identifierName,
                    default,
                    propertyOperation,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Collects property, indexer, and event writes.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol, operation, and value
        /// resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving accessor target nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving accessor edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryWriteAccesses(
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
                if (IsSummaryNestedInitializerAssignment(
                        assignment))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        assignment.Left);

                if (symbolInfo.Symbol
                        is IPropertySymbol propertySymbol &&
                    propertySymbol.SetMethod
                        is IMethodSymbol setterSymbol)
                {
                    AddSummaryPropertyWriteEdge(
                        propertySymbol,
                        setterSymbol,
                        assignment.Left,
                        GetAssignedValueExpression(
                            assignment),
                        assignment,
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);

                    continue;
                }

                if (symbolInfo.Symbol
                    is IEventSymbol eventSymbol)
                {
                    AnalyzeSummaryEventAssignment(
                        eventSymbol,
                        assignment,
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);
                }
            }

            foreach (PrefixUnaryExpressionSyntax prefixExpression
                     in GetSummaryDescendantsAndSelf
                         <PrefixUnaryExpressionSyntax>(node))
            {
                if (!prefixExpression.IsKind(
                        SyntaxKind.PreIncrementExpression) &&
                    !prefixExpression.IsKind(
                        SyntaxKind.PreDecrementExpression))
                {
                    continue;
                }

                AnalyzeSummaryUnaryPropertyWrite(
                    prefixExpression.Operand,
                    prefixExpression,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            foreach (PostfixUnaryExpressionSyntax postfixExpression
                     in GetSummaryDescendantsAndSelf
                         <PostfixUnaryExpressionSyntax>(node))
            {
                if (!postfixExpression.IsKind(
                        SyntaxKind.PostIncrementExpression) &&
                    !postfixExpression.IsKind(
                        SyntaxKind.PostDecrementExpression))
                {
                    continue;
                }

                AnalyzeSummaryUnaryPropertyWrite(
                    postfixExpression.Operand,
                    postfixExpression,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Adds getter edges for a property or indexer access, including every
        /// known compatible runtime accessor implementation.
        /// </summary>
        /// <param name="propertySymbol">
        /// The accessed property or indexer.
        /// </param>
        /// <param name="sourceNode">
        /// The source node representing the access.
        /// </param>
        /// <param name="arguments">
        /// The explicit indexer arguments, or an empty list for a normal
        /// property.
        /// </param>
        /// <param name="propertyOperation">
        /// The Roslyn property-reference operation, or
        /// <see langword="null"/> when no operation is available.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for argument, receiver, and value analysis.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving getter edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryPropertyGetterEdge(
            IPropertySymbol propertySymbol,
            SyntaxNode sourceNode,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            IPropertyReferenceOperation? propertyOperation,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (propertySymbol.GetMethod
                is not IMethodSymbol getterSymbol)
            {
                ExceptionFlowCallContext propertyContext =
                    new(propertySymbol);

                ExceptionFlowCallableKey propertyKey =
                    new(
                        propertySymbol,
                        propertyContext.Key);

                graph.GetOrAdd(
                    propertyKey,
                    propertyContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        propertyKey,
                        CreatePathStep(
                            propertySymbol.IsIndexer
                                ? ExceptionFlowPathStepKind
                                    .IndexerGetter
                                : ExceptionFlowPathStepKind
                                    .PropertyGetter,
                            propertySymbol,
                            sourceNode)));

                return;
            }

            ExceptionFlowCallContext getterContext =
                CreateCallContext(
                    getterSymbol,
                    arguments,
                    semanticModel,
                    callContext);

            INamedTypeSymbol? exactReceiverType =
                GetSummaryAccessorExactReceiverType(
                    sourceNode,
                    propertyOperation?.Instance,
                    semanticModel);

            AddSummaryAccessorCallEdges(
                getterSymbol,
                getterContext,
                propertySymbol.IsIndexer
                    ? ExceptionFlowPathStepKind.IndexerGetter
                    : ExceptionFlowPathStepKind.PropertyGetter,
                propertySymbol,
                sourceNode,
                propertyOperation?.Instance,
                exactReceiverType,
                IsSummaryBaseAccessorAccess(
                    sourceNode),
                omitImplicitTargets: false,
                semanticContext,
                graph,
                fragment);
        }

        /// <summary>
        /// Analyzes a property write performed by an increment or decrement
        /// expression.
        /// </summary>
        /// <param name="operand">
        /// The incremented or decremented expression.
        /// </param>
        /// <param name="sourceNode">
        /// The complete unary expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol, operation, and value
        /// resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving setter nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving setter edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known in the caller.
        /// </param>
        private static void AnalyzeSummaryUnaryPropertyWrite(
            ExpressionSyntax operand,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    operand);

            if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol ||
                propertySymbol.SetMethod
                    is not IMethodSymbol setterSymbol)
            {
                return;
            }

            AddSummaryPropertyWriteEdge(
                propertySymbol,
                setterSymbol,
                operand,
                valueExpression: null,
                sourceNode,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Adds property or indexer setter or init edges, including every
        /// known compatible runtime accessor implementation.
        /// </summary>
        /// <param name="propertySymbol">
        /// The written property or indexer.
        /// </param>
        /// <param name="setterSymbol">
        /// The setter or init accessor selected by compile-time binding.
        /// </param>
        /// <param name="accessExpression">
        /// The property or indexer expression being written.
        /// </param>
        /// <param name="valueExpression">
        /// The expression supplied as the final value, or
        /// <see langword="null"/> when the computed value is not statically
        /// represented by one expression.
        /// </param>
        /// <param name="sourceNode">
        /// The complete write operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation and value analysis.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving accessor targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving accessor edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known in the caller.
        /// </param>
        private static void AddSummaryPropertyWriteEdge(
            IPropertySymbol propertySymbol,
            IMethodSymbol setterSymbol,
            ExpressionSyntax accessExpression,
            ExpressionSyntax? valueExpression,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            SeparatedSyntaxList<ArgumentSyntax> indexArguments =
                GetSummaryIndexerArguments(
                    accessExpression);

            ExceptionFlowCallContext setterContext =
                CreateAccessorCallContext(
                    setterSymbol,
                    indexArguments,
                    valueExpression,
                    semanticModel,
                    callContext);

            IPropertyReferenceOperation? propertyOperation =
                semanticModel.GetOperation(
                    accessExpression)
                as IPropertyReferenceOperation;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryAccessorExactReceiverType(
                    sourceNode,
                    propertyOperation?.Instance,
                    semanticModel);

            ExceptionFlowPathStepKind stepKind =
                GetSummaryPropertyWriteStepKind(
                    propertySymbol,
                    setterSymbol);

            AddSummaryAccessorCallEdges(
                setterSymbol,
                setterContext,
                stepKind,
                propertySymbol,
                sourceNode,
                propertyOperation?.Instance,
                exactReceiverType,
                IsSummaryBaseAccessorAccess(
                    accessExpression),
                omitImplicitTargets: false,
                semanticContext,
                graph,
                fragment);
        }

        /// <summary>
        /// Gets the path-step kind for a property or indexer write.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property or indexer being written.
        /// </param>
        /// <param name="setterSymbol">
        /// The selected setter or init accessor.
        /// </param>
        /// <returns>
        /// The corresponding property, indexer, setter, or init step kind.
        /// </returns>
        private static ExceptionFlowPathStepKind
            GetSummaryPropertyWriteStepKind(
                IPropertySymbol propertySymbol,
                IMethodSymbol setterSymbol)
        {
            if (propertySymbol.IsIndexer)
            {
                return setterSymbol.IsInitOnly
                    ? ExceptionFlowPathStepKind.IndexerInit
                    : ExceptionFlowPathStepKind.IndexerSetter;
            }

            return setterSymbol.IsInitOnly
                ? ExceptionFlowPathStepKind.PropertyInit
                : ExceptionFlowPathStepKind.PropertySetter;
        }

        /// <summary>
        /// Gets the index arguments belonging to a property write.
        /// </summary>
        /// <param name="accessExpression">
        /// The written property or indexer expression.
        /// </param>
        /// <returns>
        /// The explicit index arguments, or an empty list for a normal
        /// property.
        /// </returns>
        private static SeparatedSyntaxList<ArgumentSyntax>
            GetSummaryIndexerArguments(
                ExpressionSyntax accessExpression)
        {
            if (accessExpression
                is ElementAccessExpressionSyntax elementAccess)
            {
                return elementAccess.ArgumentList.Arguments;
            }

            if (accessExpression
                is ImplicitElementAccessSyntax implicitElementAccess)
            {
                return implicitElementAccess.ArgumentList.Arguments;
            }

            return default;
        }

        /// <summary>
        /// Gets the expression representing the value sent to a setter when
        /// that value can be mapped directly from the syntax.
        /// </summary>
        /// <param name="assignment">
        /// The assignment expression.
        /// </param>
        /// <returns>
        /// The right-hand expression for simple and null-coalescing
        /// assignments; otherwise <see langword="null"/>.
        /// </returns>
        private static ExpressionSyntax? GetAssignedValueExpression(
            AssignmentExpressionSyntax assignment)
        {
            if (assignment.IsKind(
                    SyntaxKind.SimpleAssignmentExpression) ||
                assignment.IsKind(
                    SyntaxKind.CoalesceAssignmentExpression))
            {
                return assignment.Right;
            }

            return null;
        }

        /// <summary>
        /// Analyzes an event subscription or unsubscription, including every
        /// known compatible runtime event-accessor implementation.
        /// </summary>
        /// <param name="eventSymbol">
        /// The written event.
        /// </param>
        /// <param name="assignment">
        /// The add or subtract assignment.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation and value analysis.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// accessor implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving event accessor nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known in the caller.
        /// </param>
        private static void AnalyzeSummaryEventAssignment(
            IEventSymbol eventSymbol,
            AssignmentExpressionSyntax assignment,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            bool isAdd =
                assignment.IsKind(
                    SyntaxKind.AddAssignmentExpression);

            bool isRemove =
                assignment.IsKind(
                    SyntaxKind.SubtractAssignmentExpression);

            if (!isAdd &&
                !isRemove)
            {
                return;
            }

            IMethodSymbol? accessorSymbol =
                isAdd
                    ? eventSymbol.AddMethod
                    : eventSymbol.RemoveMethod;

            if (accessorSymbol == null)
            {
                AddSummaryUncertainEventAccessor(
                    eventSymbol,
                    isAdd,
                    fragment);

                return;
            }

            ExceptionFlowCallContext accessorContext =
                CreateAccessorCallContext(
                    accessorSymbol,
                    default,
                    assignment.Right,
                    semanticModel,
                    callContext);

            IEventAssignmentOperation? eventAssignmentOperation =
                semanticModel.GetOperation(
                    assignment)
                as IEventAssignmentOperation;

            IEventReferenceOperation? eventReferenceOperation =
                eventAssignmentOperation?.EventReference
                    as IEventReferenceOperation;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryAccessorExactReceiverType(
                    assignment,
                    eventReferenceOperation?.Instance,
                    semanticModel);

            ExceptionFlowPathStepKind stepKind =
                isAdd
                    ? ExceptionFlowPathStepKind.EventAdd
                    : ExceptionFlowPathStepKind.EventRemove;

            AddSummaryAccessorCallEdges(
                accessorSymbol,
                accessorContext,
                stepKind,
                eventSymbol,
                assignment,
                eventReferenceOperation?.Instance,
                exactReceiverType,
                IsSummaryBaseAccessorAccess(
                    assignment.Left),
                omitImplicitTargets: true,
                semanticContext,
                graph,
                fragment);
        }

        /// <summary>
        /// Adds uncertainty for an event accessor that could not be resolved.
        /// </summary>
        /// <param name="eventSymbol">
        /// The unresolved event.
        /// </param>
        /// <param name="isAdd">
        /// Whether the missing accessor is the add accessor.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        private static void AddSummaryUncertainEventAccessor(
            IEventSymbol eventSymbol,
            bool isAdd,
            ExceptionFlowSummaryFragment fragment)
        {
            string displayName =
                eventSymbol.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                displayName =
                    eventSymbol.Name;
            }

            string accessorName =
                isAdd
                    ? "add"
                    : "remove";

            fragment.AddUncertainTarget(
                $"{displayName}.{accessorName}");
        }
    }
}
