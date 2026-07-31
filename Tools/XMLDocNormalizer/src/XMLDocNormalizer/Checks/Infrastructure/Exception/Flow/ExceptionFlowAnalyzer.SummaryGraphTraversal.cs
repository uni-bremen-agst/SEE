using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains local syntax traversal for callable summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes one syntax node into a local summary fragment.
        /// </summary>
        /// <param name="node">The syntax node to analyze.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving newly discovered callable nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (node is TryStatementSyntax tryStatement)
            {
                AnalyzeSummaryTryStatement(
                    tryStatement,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                return;
            }

            AnalyzeSummarySimpleNode(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            foreach (TryStatementSyntax nestedTry
                     in GetNestedSummaryTryStatements(
                         node))
            {
                AnalyzeSummaryTryStatement(
                    nestedTry,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Analyzes one node while excluding nested try-statements and nested
        /// callable declarations.
        /// </summary>
        /// <param name="node">The syntax node to analyze.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving newly discovered callable nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummarySimpleNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            AnalyzeSummaryThrows(
                node,
                semanticModel,
                fragment,
                callContext);

            AnalyzeSummaryInvocations(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryDynamicBindings(
                node,
                semanticModel,
                fragment);

            AnalyzeSummaryObjectCreations(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryImplicitObjectCreations(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryPropertyAndIndexerAccesses(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            AnalyzeSummarySimpleNamePropertyAccesses(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryWriteAccesses(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryOperatorsAndConversions(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryDeconstructions(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryCollectionInitializers(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryDisposals(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryForEachOperations(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);

            AnalyzeSummaryAwaitOperations(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Analyzes a try-statement into a filterable local fragment.
        /// </summary>
        /// <param name="tryStatement">
        /// The try-statement to analyze.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for catch-type resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving newly discovered callable nodes.
        /// </param>
        /// <param name="fragment">
        /// The containing local fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryTryStatement(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowSummaryFragment tryFragment =
                new();

            AnalyzeSummaryNode(
                tryStatement.Block,
                semanticModel,
                semanticContext,
                graph,
                tryFragment,
                callContext);

            SuppressCaughtSummaryFlow(
                tryStatement,
                semanticModel,
                tryFragment);

            fragment.Merge(
                tryFragment);

            foreach (CatchClauseSyntax catchClause
                     in tryStatement.Catches)
            {
                if (catchClause.Filter != null)
                {
                    AnalyzeSummaryNode(
                        catchClause.Filter.FilterExpression,
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);
                }

                AnalyzeSummaryNode(
                    catchClause.Block,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            if (tryStatement.Finally != null)
            {
                AnalyzeSummaryNode(
                    tryStatement.Finally.Block,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Applies the handling semantics of catch clauses to a protected
        /// summary fragment.
        /// </summary>
        /// <param name="tryStatement">
        /// The try-statement whose catch clauses should be evaluated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for caught-type resolution.
        /// </param>
        /// <param name="tryFragment">
        /// The protected local summary fragment.
        /// </param>
        private static void SuppressCaughtSummaryFlow(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment tryFragment)
        {
            foreach (CatchClauseSyntax catchClause
                     in tryStatement.Catches)
            {
                if (!CatchSuppressesOriginalException(
                        catchClause,
                        semanticModel) ||
                    catchClause.Filter != null)
                {
                    continue;
                }

                if (IsCatchAll(
                        catchClause,
                        semanticModel))
                {
                    tryFragment.SuppressAll();
                    return;
                }

                if (GetCaughtExceptionType(
                        catchClause,
                        semanticModel)
                    is INamedTypeSymbol caughtType)
                {
                    tryFragment.SuppressCaughtException(
                        caughtType);
                }
            }
        }

        /// <summary>
        /// Returns nested try-statements that belong to the currently
        /// analyzed callable.
        /// </summary>
        /// <param name="node">
        /// The current callable body or expression.
        /// </param>
        /// <returns>
        /// Nested try-statements excluding those declared inside local
        /// functions, lambdas, and anonymous methods.
        /// </returns>
        private static IEnumerable<TryStatementSyntax>
            GetNestedSummaryTryStatements(
                SyntaxNode node)
        {
            return node.DescendantNodes(
                    descendIntoChildren:
                        child =>
                            child is not TryStatementSyntax &&
                            child is not LocalFunctionStatementSyntax &&
                            child is not
                                AnonymousFunctionExpressionSyntax)
                .OfType<TryStatementSyntax>();
        }

        /// <summary>
        /// Returns matching descendants that belong to the currently
        /// analyzed callable.
        /// </summary>
        /// <typeparam name="TNode">
        /// The syntax-node type to return.
        /// </typeparam>
        /// <param name="node">
        /// The current callable body or expression.
        /// </param>
        /// <returns>
        /// Matching nodes excluding nested try-statements and bodies of local
        /// functions, lambdas, and anonymous methods.
        /// </returns>
        private static IEnumerable<TNode>
            GetSummaryDescendantsAndSelf<TNode>(
                SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.DescendantNodesAndSelf(
                    descendIntoChildren:
                        child =>
                            ReferenceEquals(
                                child,
                                node) ||
                            child is not TryStatementSyntax &&
                            child is not LocalFunctionStatementSyntax &&
                            child is not
                                AnonymousFunctionExpressionSyntax)
                .OfType<TNode>()
                .Where(
                    ShouldIncludeSummaryNode);
        }

        /// <summary>
        /// Determines whether a matching syntax node represents an operation
        /// that should be included in the current callable summary.
        /// </summary>
        /// <typeparam name="TNode">
        /// The matching syntax-node type.
        /// </typeparam>
        /// <param name="candidate">
        /// The candidate node.
        /// </param>
        /// <returns>
        /// <see langword="false"/> for a pure write target of a simple
        /// assignment; otherwise <see langword="true"/>.
        /// </returns>
        private static bool ShouldIncludeSummaryNode<TNode>(
            TNode candidate)
            where TNode : SyntaxNode
        {
            return candidate
                       is not ExpressionSyntax expression ||
                   !IsWriteOnlySummaryAccess(
                       expression);
        }

        /// <summary>
        /// Determines whether an expression is the pure write target of a
        /// simple assignment.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is the exact left side of
        /// a simple assignment; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsWriteOnlySummaryAccess(
            ExpressionSyntax expression)
        {
            return expression.Parent
                       is AssignmentExpressionSyntax assignment &&
                   assignment.IsKind(
                       SyntaxKind.SimpleAssignmentExpression) &&
                   ReferenceEquals(
                       assignment.Left,
                       expression);
        }
    }
}
