using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides stateless syntax and symbol-usage queries shared by call-context
    /// construction and value-fact discovery.
    /// </summary>
    internal static class ExceptionFlowSymbolUsageFacts
    {
        /// <summary>
        /// Determines whether an expression writes to the specified symbol.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="symbol">The symbol whose writes are detected.</param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression may write the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool ExpressionWritesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExceptionFlowDataFlowFactsProvider.ExceptionFlowDataFlowFacts dataFlow =
                ExceptionFlowDataFlowFactsProvider.GetFacts(expression, semanticModel);

            return dataFlow.Succeeded
                && dataFlow.WrittenInside.Any(
                    writtenSymbol =>
                        SymbolEqualityComparer.Default.Equals(
                            writtenSymbol,
                            symbol));
        }

        /// <summary>
        /// Determines whether an expression resolves to the specified symbol.
        /// </summary>
        /// <param name="expression">The expression to resolve.</param>
        /// <param name="symbol">The expected symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression references the specified
        /// symbol; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool ExpressionReferencesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            return symbolInfo.Symbol != null
                && SymbolEqualityComparer.Default.Equals(
                    symbolInfo.Symbol,
                    symbol);
        }

        /// <summary>
        /// Removes surrounding parenthesized expressions.
        /// </summary>
        /// <param name="expression">The expression to unwrap.</param>
        /// <returns>The innermost non-parenthesized expression.</returns>
        internal static ExpressionSyntax UnwrapParenthesizedExpression(
            ExpressionSyntax expression)
        {
            ExpressionSyntax currentExpression = expression;

            while (currentExpression
                   is ParenthesizedExpressionSyntax parenthesized)
            {
                currentExpression = parenthesized.Expression;
            }

            return currentExpression;
        }
    }
}
