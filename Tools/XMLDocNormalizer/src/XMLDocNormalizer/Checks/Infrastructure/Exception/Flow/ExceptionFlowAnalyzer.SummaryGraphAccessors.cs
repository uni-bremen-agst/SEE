using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving getter edges.
        /// </param>
        private static void AnalyzeSummarySimpleNamePropertyAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
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

                AddSummaryPropertyGetterEdge(
                    propertySymbol,
                    identifierName,
                    graph,
                    fragment);
            }
        }

        /// <summary>
        /// Collects property, indexer, and event writes.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value resolution.
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
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Adds a getter edge for an unqualified property access.
        /// </summary>
        /// <param name="propertySymbol">
        /// The accessed property.
        /// </param>
        /// <param name="sourceNode">
        /// The source node representing the access.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the getter node.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving the getter edge.
        /// </param>
        private static void AddSummaryPropertyGetterEdge(
            IPropertySymbol propertySymbol,
            SyntaxNode sourceNode,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
            ISymbol targetSymbol;

            if (propertySymbol.GetMethod
                is IMethodSymbol getterSymbol)
            {
                targetSymbol =
                    getterSymbol;
            }
            else
            {
                targetSymbol =
                    propertySymbol;
            }

            ExceptionFlowCallContext targetContext =
                new(targetSymbol);

            ExceptionFlowCallableKey targetKey =
                new(
                    targetSymbol,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        ExceptionFlowPathStepKind.PropertyGetter,
                        propertySymbol,
                        sourceNode)));
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
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the setter node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the setter edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known in the caller.
        /// </param>
        private static void AnalyzeSummaryUnaryPropertyWrite(
            ExpressionSyntax operand,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
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
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Adds one property or indexer setter or init edge.
        /// </summary>
        /// <param name="propertySymbol">
        /// The written property or indexer.
        /// </param>
        /// <param name="setterSymbol">
        /// The selected setter or init accessor.
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
        /// The semantic model used for value analysis.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the accessor target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the edge.
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
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            SeparatedSyntaxList<ArgumentSyntax> indexArguments =
                GetSummaryIndexerArguments(
                    accessExpression);

            ExceptionFlowCallContext targetContext =
                CreateAccessorCallContext(
                    setterSymbol,
                    indexArguments,
                    valueExpression,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    setterSymbol,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            ExceptionFlowPathStepKind stepKind =
                GetSummaryPropertyWriteStepKind(
                    propertySymbol,
                    setterSymbol);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        propertySymbol,
                        sourceNode)));
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
        /// Analyzes an event subscription or unsubscription.
        /// </summary>
        /// <param name="eventSymbol">
        /// The written event.
        /// </param>
        /// <param name="assignment">
        /// The add or subtract assignment.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value analysis.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the event accessor node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the edge or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known in the caller.
        /// </param>
        private static void AnalyzeSummaryEventAssignment(
            IEventSymbol eventSymbol,
            AssignmentExpressionSyntax assignment,
            SemanticModel semanticModel,
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

            if (accessorSymbol.IsImplicitlyDeclared)
            {
                return;
            }

            ExceptionFlowCallContext targetContext =
                CreateAccessorCallContext(
                    accessorSymbol,
                    default,
                    assignment.Right,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    accessorSymbol,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            ExceptionFlowPathStepKind stepKind =
                isAdd
                    ? ExceptionFlowPathStepKind.EventAdd
                    : ExceptionFlowPathStepKind.EventRemove;

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        eventSymbol,
                        assignment)));
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
