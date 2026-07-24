using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Performs direct and transitive analysis of exceptions that may escape from a member.
    /// </summary>
    /// <remarks>
    /// The analysis is conservative and attempts to suppress exceptions that are fully handled
    /// by surrounding catch-clauses. Catch filters are treated conservatively and therefore do
    /// not suppress the caught exception flow.
    /// </remarks>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines how exception flow should be traversed.
        /// </summary>
        private enum ExceptionFlowTraversalMode
        {
            /// <summary>
            /// Only explicit throw operations and modeled framework throw helpers
            /// inside the analyzed member are considered.
            /// </summary>
            Direct,

            /// <summary>
            /// Exceptions are analyzed transitively through invoked members and other reachable constructs.
            /// </summary>
            Transitive
        }

        /// <summary>
        /// Analyzes all exception types that may escape directly from the specified member.
        /// Direct exception sources include explicit throw operations and modeled framework
        /// throw helpers. Exceptions that are fully caught and handled within the member
        /// are suppressed.
        /// </summary>
        /// <param name="member">The member whose direct exception flow should be analyzed.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// A result object containing all proven directly escaping exception types.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeDirectlyThrownExceptions(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext)
        {
            ExceptionFlowAnalysisResult result = new();

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(
                    member,
                    out SyntaxNode? body) ||
                body == null)
            {
                return result;
            }

            ExceptionFlowTraversalState traversalState = new();

            ExceptionFlowCallContext callContext =
                CreateRootCallContext(
                    member,
                    semanticModel);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                ExceptionFlowTraversalMode.Direct,
                callContext);

            return result;
        }

        /// <summary>
        /// Analyzes all exception types that may escape directly or transitively from the specified member.
        /// Exceptions that are fully caught and handled within the analyzed member bodies are suppressed.
        /// </summary>
        /// <param name="member">The member whose transitive exception flow should be analyzed.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// A result object containing all proven transitively escaping exception types and any uncertainty
        /// that could not be resolved safely.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext)
        {
            ExceptionFlowAnalysisResult result = new();

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            if (!SyntaxUtils.TryGetMemberBody(
                    member,
                    out SyntaxNode? body) ||
                body == null)
            {
                return result;
            }

            ExceptionFlowTraversalState traversalState = new();

            ExceptionFlowCallContext callContext =
                CreateRootCallContext(
                    member,
                    semanticModel);

            AnalyzeNode(
                body,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                ExceptionFlowTraversalMode.Transitive,
                callContext);

            return result;
        }

        /// <summary>
        /// Analyzes a syntax node and all nested try-statements below it.
        /// Nested try-statements are processed separately so that catch-based suppression can be applied.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            if (node is TryStatementSyntax tryStatement)
            {
                AnalyzeTryStatement(
                    tryStatement,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
                return;
            }

            AnalyzeSimpleNode(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                mode,
                callContext);

            foreach (TryStatementSyntax nestedTry in GetNestedTryStatements(node))
            {
                AnalyzeTryStatement(
                    nestedTry,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
            }
        }

        /// <summary>
        /// Analyzes a syntax node excluding nested try-statements.
        /// </summary>
        /// <param name="node">The node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeSimpleNode(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            AnalyzeThrows(
                node,
                semanticModel,
                result,
                callContext);

            AnalyzeInvocations(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                mode,
                callContext);

            if (mode == ExceptionFlowTraversalMode.Direct)
            {
                return;
            }

            AnalyzeObjectCreations(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                callContext);

            AnalyzePropertyAndIndexerAccesses(
                node,
                semanticModel,
                semanticContext,
                result,
                traversalState,
                callContext);
        }

        /// <summary>
        /// Merges one exception-flow result into another.
        /// </summary>
        /// <param name="target">The target result.</param>
        /// <param name="source">The source result.</param>
        private static void MergeResults(
            ExceptionFlowAnalysisResult target,
            ExceptionFlowAnalysisResult source)
        {
            target.Merge(source);
        }

        /// <summary>
        /// Returns all nested try-statements below the specified node without descending into
        /// nested try-statements more than once.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <returns>An enumeration of nested try-statements.</returns>
        private static IEnumerable<TryStatementSyntax> GetNestedTryStatements(SyntaxNode node)
        {
            return node.DescendantNodes(
                    descendIntoChildren: child => child is not TryStatementSyntax)
                .OfType<TryStatementSyntax>();
        }

        /// <summary>
        /// Returns all nodes of the given type below the specified node while excluding
        /// content inside nested try-statements.
        /// </summary>
        /// <typeparam name="TNode">The node type to return.</typeparam>
        /// <param name="node">The root node.</param>
        /// <returns>An enumeration of matching nodes.</returns>
        private static IEnumerable<TNode> GetDescendantsAndSelfExcludingNestedTry<TNode>(
            SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.DescendantNodesAndSelf(
                    descendIntoChildren: child =>
                        ReferenceEquals(child, node) ||
                        child is not TryStatementSyntax)
                .OfType<TNode>();
        }

        /// <summary>
        /// Collects exception types that are thrown directly within the specified node,
        /// excluding nested try-statements and throws in branches proven unreachable
        /// by the current call-site facts.
        /// </summary>
        /// <param name="node">
        /// The node to inspect for throw statements and throw expressions.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value-fact resolution.
        /// </param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
        private static void AnalyzeThrows(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowCallContext callContext)
        {
            foreach (ThrowStatementSyntax throwStatement
                     in GetDescendantsAndSelfExcludingNestedTry
                         <ThrowStatementSyntax>(node))
            {
                if (IsThrowStatementProvenUnreachable(
                        throwStatement,
                        node,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                AddThrownExceptionType(
                    result,
                    semanticModel,
                    throwStatement.Expression);
            }

            foreach (ThrowExpressionSyntax throwExpression
                     in GetDescendantsAndSelfExcludingNestedTry
                         <ThrowExpressionSyntax>(node))
            {
                if (IsThrowExpressionProvenUnreachable(
                        throwExpression,
                        node,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                AddThrownExceptionType(
                    result,
                    semanticModel,
                    throwExpression.Expression);
            }
        }

        /// <summary>
        /// Resolves the exception type from a thrown expression and adds it to the result
        /// if it represents an object creation of a named type.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="expression">The thrown expression to inspect.</param>
        private static void AddThrownExceptionType(
            ExceptionFlowAnalysisResult result,
            SemanticModel semanticModel,
            ExpressionSyntax? expression)
        {
            if (expression is not ObjectCreationExpressionSyntax creation)
            {
                return;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(creation.Type);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                result.AddThrownException(typeSymbol);
            }
        }
    }
}
