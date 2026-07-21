using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains control-flow reasoning for terminating null guards.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether reaching the specified expression proves that a local variable
        /// is non-null because an earlier null guard terminates the current control-flow path.
        /// </summary>
        /// <param name="expression">The local-variable expression being evaluated.</param>
        /// <param name="localSymbol">The local symbol to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if an earlier terminating guard proves the local variable
        /// to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalProvenNonNullByPrecedingGuard(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement == null ||
                currentStatement.Parent is not BlockSyntax containingBlock)
            {
                return false;
            }

            int currentStatementIndex =
                containingBlock.Statements.IndexOf(currentStatement);

            if (currentStatementIndex < 0)
            {
                return false;
            }

            for (int index = currentStatementIndex - 1;
                 index >= 0;
                 index--)
            {
                if (containingBlock.Statements[index]
                    is not IfStatementSyntax ifStatement)
                {
                    continue;
                }

                if (!StatementAlwaysTerminatesCurrentPath(
                        ifStatement.Statement))
                {
                    continue;
                }

                if (ConditionBeingFalseProvesLocalNonNull(
                        ifStatement.Condition,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a condition evaluating to <see langword="false"/> proves
        /// that the specified local variable is non-null.
        /// </summary>
        /// <param name="condition">The condition to inspect.</param>
        /// <param name="localSymbol">The local symbol whose null state is evaluated.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the false condition result proves the local variable
        /// to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ConditionBeingFalseProvesLocalNonNull(
            ExpressionSyntax condition,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr &&
                logicalOr.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return ConditionBeingFalseProvesLocalNonNull(
                           logicalOr.Left,
                           localSymbol,
                           semanticModel) ||
                       ConditionBeingFalseProvesLocalNonNull(
                           logicalOr.Right,
                           localSymbol,
                           semanticModel);
            }

            return IsLocalComparedEqualToNull(
                       unwrappedCondition,
                       localSymbol,
                       semanticModel) ||
                   IsLocalMatchedAgainstNullPattern(
                       unwrappedCondition,
                       localSymbol,
                       semanticModel);
        }

        /// <summary>
        /// Determines whether an expression compares the specified local variable
        /// to <see langword="null"/> using the equality operator.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression is an equality comparison between
        /// the local variable and <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsLocalComparedEqualToNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            if (expression is not BinaryExpressionSyntax comparison ||
                !comparison.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            if (comparison.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesLocal(
                    comparison.Right,
                    localSymbol,
                    semanticModel);
            }

            if (comparison.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesLocal(
                    comparison.Left,
                    localSymbol,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression matches the specified local variable against
        /// the constant <see langword="null"/> pattern.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression has the form
        /// <c>local is null</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalMatchedAgainstNullPattern(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            if (expression is not IsPatternExpressionSyntax isPatternExpression ||
                isPatternExpression.Pattern
                    is not ConstantPatternSyntax constantPattern ||
                !constantPattern.Expression.IsKind(
                    SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            return ExpressionReferencesLocal(
                isPatternExpression.Expression,
                localSymbol,
                semanticModel);
        }

        /// <summary>
        /// Determines whether an expression resolves to the specified local symbol.
        /// </summary>
        /// <param name="expression">The expression to resolve.</param>
        /// <param name="localSymbol">The expected local symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression references the specified local;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ExpressionReferencesLocal(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            return symbolInfo.Symbol is ILocalSymbol referencedLocal &&
                   SymbolEqualityComparer.Default.Equals(
                       referencedLocal,
                       localSymbol);
        }

        /// <summary>
        /// Removes surrounding parenthesized expressions.
        /// </summary>
        /// <param name="expression">The expression to unwrap.</param>
        /// <returns>The innermost non-parenthesized expression.</returns>
        private static ExpressionSyntax UnwrapParenthesizedExpression(
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
        /// Determines whether a statement always terminates the current control-flow path.
        /// </summary>
        /// <param name="statement">The statement to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if execution cannot continue with the next statement
        /// in the containing block; otherwise <see langword="false"/>.
        /// </returns>
        private static bool StatementAlwaysTerminatesCurrentPath(
            StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax or
                ThrowStatementSyntax or
                ContinueStatementSyntax or
                BreakStatementSyntax or
                GotoStatementSyntax)
            {
                return true;
            }

            if (statement is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    return false;
                }

                return StatementAlwaysTerminatesCurrentPath(
                    block.Statements[block.Statements.Count - 1]);
            }

            return false;
        }
    }
}
