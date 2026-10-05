using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExceptionFlowDataFlowFacts = XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowDataFlowFactsProvider.ExceptionFlowDataFlowFacts;

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
        /// Determines whether a statement writes to the specified symbol.
        /// </summary>
        /// <param name="statement">The statement to inspect.</param>
        /// <param name="symbol">The symbol whose writes are detected.</param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the statement may write the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool StatementWritesSymbol(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExceptionFlowDataFlowFactsProvider.ExceptionFlowDataFlowFacts dataFlow =
                ExceptionFlowDataFlowFactsProvider.GetFacts(statement, semanticModel);

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
        /// Gets the nearest preceding straight-line assignment to a local.
        /// </summary>
        /// <param name="expression">
        /// The expression whose preceding statements are inspected.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol whose assignment is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow and symbol analysis.
        /// </param>
        /// <param name="assignedExpression">
        /// Receives the right-hand expression of the nearest qualifying
        /// assignment when one is found; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a qualifying preceding assignment is
        /// found; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetPrecedingSimpleLocalAssignment(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? assignedExpression)
        {
            assignedExpression = null;

            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement?.Parent
                    is not BlockSyntax containingBlock)
            {
                return false;
            }

            int currentIndex =
                containingBlock.Statements.IndexOf(
                    currentStatement);

            if (currentIndex < 0)
            {
                return false;
            }

            for (int index = currentIndex - 1;
                 index >= 0;
                 index--)
            {
                StatementSyntax precedingStatement =
                    containingBlock.Statements[index];

                ExceptionFlowDataFlowFacts dataFlow =
                    ExceptionFlowDataFlowFactsProvider.GetFacts(
                        precedingStatement,
                        semanticModel);

                if (!dataFlow.Succeeded)
                {
                    return false;
                }

                bool writesLocal =
                    dataFlow.WrittenInside.Any(
                        writtenSymbol =>
                            SymbolEqualityComparer.Default.Equals(
                                writtenSymbol,
                                localSymbol));

                if (!writesLocal)
                {
                    continue;
                }

                if (precedingStatement
                        is not ExpressionStatementSyntax expressionStatement
                    || expressionStatement.Expression
                        is not AssignmentExpressionSyntax assignment
                    || !assignment.IsKind(
                        SyntaxKind.SimpleAssignmentExpression)
                    || !ExpressionReferencesSymbol(
                        assignment.Left,
                        localSymbol,
                        semanticModel))
                {
                    return false;
                }

                assignedExpression =
                    assignment.Right;

                return true;
            }

            return false;
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

        /// <summary>
        /// Resolves the symbol introduced or referenced by an out argument.
        /// </summary>
        /// <param name="argument">The out argument.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the argument.
        /// </param>
        /// <returns>
        /// The corresponding local symbol, or <see langword="null"/> when no
        /// supported local could be resolved.
        /// </returns>
        internal static ISymbol? GetOutArgumentSymbol(
            ArgumentSyntax argument,
            SemanticModel semanticModel)
        {
            if (argument.Expression
                    is DeclarationExpressionSyntax declarationExpression
                && declarationExpression.Designation
                    is SingleVariableDesignationSyntax designation)
            {
                return semanticModel.GetDeclaredSymbol(designation);
            }

            return semanticModel.GetSymbolInfo(argument.Expression).Symbol;
        }

        /// <summary>
        /// Determines whether an assignment writes an alias back into the same
        /// dictionary property from which it originated.
        /// </summary>
        /// <param name="targetExpression">The assignment target.</param>
        /// <param name="dictionaryProperty">
        /// The expected dictionary property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for property resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the target is an indexer on the same
        /// dictionary property; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool AssignmentTargetsDictionaryProperty(
            ExpressionSyntax targetExpression,
            IPropertySymbol dictionaryProperty,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedTarget =
                UnwrapParenthesizedExpression(targetExpression);

            if (unwrappedTarget
                    is not ElementAccessExpressionSyntax elementAccess)
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(elementAccess.Expression);

            return symbolInfo.Symbol is IPropertySymbol targetProperty
                && SymbolEqualityComparer.Default.Equals(
                    targetProperty.OriginalDefinition,
                    dictionaryProperty.OriginalDefinition);
        }
    }
}
