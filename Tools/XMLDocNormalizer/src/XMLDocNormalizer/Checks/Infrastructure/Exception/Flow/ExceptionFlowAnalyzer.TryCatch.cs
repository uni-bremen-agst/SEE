using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains try/catch-specific exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes a try-statement and suppresses exceptions from the try-block that are fully
        /// handled by one of its catch-clauses.
        /// </summary>
        /// <param name="tryStatement">The try-statement to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeTryStatement(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowAnalysisResult tryResult = new();

            AnalyzeNode(
                tryStatement.Block,
                semanticModel,
                semanticContext,
                tryResult,
                traversalState,
                mode,
                callContext);

            SuppressCaughtExceptionsFromTry(
                tryStatement,
                semanticModel,
                tryResult);

            MergeResults(result, tryResult);

            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                if (catchClause.Filter != null)
                {
                    AnalyzeNode(
                        catchClause.Filter.FilterExpression,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        mode,
                        callContext);
                }

                if (catchClause.Block != null)
                {
                    AnalyzeNode(
                        catchClause.Block,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        mode,
                        callContext);
                }
            }

            if (tryStatement.Finally != null)
            {
                AnalyzeNode(
                    tryStatement.Finally.Block,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
            }
        }

        /// <summary>
        /// Suppresses exceptions from a try-block that are fully handled by the associated catch-clauses.
        /// </summary>
        /// <param name="tryStatement">The try-statement whose catches should be evaluated.</param>
        /// <param name="semanticModel">The semantic model used for catch type resolution.</param>
        /// <param name="tryResult">The exception-flow result produced for the try-block.</param>
        private static void SuppressCaughtExceptionsFromTry(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult tryResult)
        {
            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                if (!CatchSuppressesOriginalException(catchClause))
                {
                    continue;
                }

                if (catchClause.Filter != null)
                {
                    continue;
                }

                if (IsCatchAll(catchClause, semanticModel))
                {
                    tryResult.ClearThrownExceptions();
                    tryResult.UncertainTargets.Clear();
                    return;
                }

                INamedTypeSymbol? caughtType =
                    GetCaughtExceptionType(catchClause, semanticModel);

                if (caughtType == null)
                {
                    continue;
                }

                tryResult.RemoveThrownExceptions(thrownType =>
                    thrownType.InheritsFromOrEquals(caughtType));
            }
        }

        /// <summary>
        /// Determines whether a catch-clause fully handles the original caught exception
        /// instead of rethrowing it.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the original exception is not rethrown by the catch-clause;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool CatchSuppressesOriginalException(
            CatchClauseSyntax catchClause)
        {
            if (catchClause.Block == null)
            {
                return true;
            }

            string? caughtIdentifier =
                catchClause.Declaration?.Identifier.ValueText;

            if (string.IsNullOrWhiteSpace(caughtIdentifier))
            {
                caughtIdentifier = null;
            }

            foreach (ThrowStatementSyntax throwStatement
                     in catchClause.Block
                         .DescendantNodesAndSelf()
                         .OfType<ThrowStatementSyntax>())
            {
                if (throwStatement.Expression == null)
                {
                    return false;
                }

                if (caughtIdentifier != null &&
                    throwStatement.Expression is IdentifierNameSyntax identifier &&
                    string.Equals(
                        identifier.Identifier.ValueText,
                        caughtIdentifier,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (ThrowExpressionSyntax throwExpression
                     in catchClause.Block
                         .DescendantNodesAndSelf()
                         .OfType<ThrowExpressionSyntax>())
            {
                if (throwExpression.Expression is IdentifierNameSyntax identifier &&
                    caughtIdentifier != null &&
                    string.Equals(
                        identifier.Identifier.ValueText,
                        caughtIdentifier,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the catch-clause catches all exceptions.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <param name="semanticModel">The semantic model used for type resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the catch-clause catches all exceptions;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsCatchAll(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration == null)
            {
                return true;
            }

            INamedTypeSymbol? caughtType =
                GetCaughtExceptionType(catchClause, semanticModel);

            if (caughtType == null)
            {
                return false;
            }

            return IsSystemExceptionType(caughtType);
        }

        /// <summary>
        /// Resolves the caught exception type of a catch-clause.
        /// </summary>
        /// <param name="catchClause">The catch-clause to inspect.</param>
        /// <param name="semanticModel">The semantic model used for type resolution.</param>
        /// <returns>
        /// The caught exception type if it can be resolved;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? GetCaughtExceptionType(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration?.Type == null)
            {
                return null;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(catchClause.Declaration.Type);

            return symbolInfo.Symbol as INamedTypeSymbol;
        }

        /// <summary>
        /// Determines whether the given type is <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is <see cref="System.Exception"/>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSystemExceptionType(
            INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(
                       SymbolDisplayFormat.FullyQualifiedFormat) ==
                   "global::System.Exception";
        }
    }
}
