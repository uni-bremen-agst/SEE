using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains control-flow reasoning for terminating value guards.
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
            return GetFactsProvenByPrecedingGuard(
                    expression,
                    localSymbol,
                    semanticModel)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        }

        /// <summary>
        /// Gets facts proven for a symbol because an earlier guard terminates the
        /// current control-flow path when its condition evaluates to
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="expression">The symbol expression being evaluated.</param>
        /// <param name="symbol">The local or parameter symbol to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>The facts proven by preceding terminating guards.</returns>
        private static ExceptionFlowValueFacts GetFactsProvenByPrecedingGuard(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts facts =
                ExceptionFlowValueFacts.None;

            while (currentStatement.Parent
                   is BlockSyntax containingBlock)
            {
                int currentStatementIndex =
                    containingBlock.Statements.IndexOf(
                        currentStatement);

                if (currentStatementIndex < 0)
                {
                    break;
                }

                bool earlierFactsInvalidated = false;

                for (int index = currentStatementIndex - 1;
                     index >= 0;
                     index--)
                {
                    StatementSyntax precedingStatement =
                        containingBlock.Statements[index];

                    facts |= GetFactsProvenBySuccessfulFrameworkGuard(
                        precedingStatement,
                        symbol,
                        semanticModel);

                    bool writesSymbol =
                        StatementWritesSymbol(
                            precedingStatement,
                            symbol,
                            semanticModel);

                    if (precedingStatement
                            is IfStatementSyntax ifStatement &&
                        ifStatement.Else == null &&
                        StatementAlwaysTerminatesCurrentPath(
                            ifStatement.Statement))
                    {
                        ExceptionFlowValueFacts guardFacts =
                            GetFactsProvenWhenConditionIsFalse(
                                ifStatement.Condition,
                                symbol,
                                semanticModel);

                        facts |= guardFacts;

                        if (writesSymbol)
                        {
                            // The guard itself may initialize the symbol, for example
                            // through an out argument. Facts derived from the false
                            // guard result describe the value after that write and
                            // therefore remain valid. Facts from statements preceding
                            // the guard must not be considered.
                            earlierFactsInvalidated = true;
                            break;
                        }

                        continue;
                    }

                    if (writesSymbol)
                    {
                        earlierFactsInvalidated = true;
                        break;
                    }
                }

                if (earlierFactsInvalidated)
                {
                    break;
                }

                currentStatement =
                    GetSafeContainingStatement(
                        containingBlock,
                        symbol,
                        semanticModel);

                if (currentStatement == null)
                {
                    break;
                }
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets value facts established when a supported framework guard returns
        /// normally without throwing.
        /// </summary>
        /// <param name="statement">The statement containing the potential guard.</param>
        /// <param name="symbol">The symbol passed to the guard.</param>
        /// <param name="semanticModel">
        /// The semantic model used for invocation and argument resolution.
        /// </param>
        /// <returns>
        /// The facts established by the successful guard invocation, or
        /// <see cref="ExceptionFlowValueFacts.None"/> if the statement is not a
        /// supported guard for the specified symbol.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetFactsProvenBySuccessfulFrameworkGuard(
                StatementSyntax statement,
                ISymbol symbol,
                SemanticModel semanticModel)
        {
            if (statement is not ExpressionStatementSyntax expressionStatement ||
                expressionStatement.Expression
                    is not InvocationExpressionSyntax invocation)
            {
                return ExceptionFlowValueFacts.None;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return ExceptionFlowValueFacts.None;
            }

            bool guardsAgainstNull =
                KnownFrameworkExceptionModel.IsArgumentNullThrowIfNull(
                    methodSymbol,
                    semanticModel.Compilation);

            bool guardsAgainstNullOrEmpty =
                KnownFrameworkExceptionModel.IsArgumentExceptionThrowIfNullOrEmpty(
                    methodSymbol,
                    semanticModel.Compilation);

            bool guardsAgainstNullOrWhiteSpace =
                KnownFrameworkExceptionModel
                    .IsArgumentExceptionThrowIfNullOrWhiteSpace(
                        methodSymbol,
                        semanticModel.Compilation);

            if (!guardsAgainstNull &&
                !guardsAgainstNullOrEmpty &&
                !guardsAgainstNullOrWhiteSpace)
            {
                return ExceptionFlowValueFacts.None;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int index = 0; index < arguments.Count; index++)
            {
                ArgumentSyntax argument = arguments[index];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        index,
                        methodSymbol);

                if (parameterIndex != 0 ||
                    !ExpressionReferencesSymbol(
                        argument.Expression,
                        symbol,
                        semanticModel))
                {
                    continue;
                }

                if (guardsAgainstNullOrWhiteSpace)
                {
                    return (
                        ExceptionFlowValueFacts.NonNull |
                        ExceptionFlowValueFacts.NonEmptyString |
                        ExceptionFlowValueFacts.NonWhiteSpaceString)
                        .Normalize();
                }

                if (guardsAgainstNullOrEmpty)
                {
                    return (
                        ExceptionFlowValueFacts.NonNull |
                        ExceptionFlowValueFacts.NonEmptyString)
                        .Normalize();
                }

                return ExceptionFlowValueFacts.NonNull;
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Gets the statement containing a nested block when value facts can safely
        /// be propagated from the surrounding block into that nested block.
        /// </summary>
        /// <param name="block">The nested block.</param>
        /// <param name="symbol">The symbol whose facts are propagated.</param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// The containing statement if propagation is supported and its condition
        /// does not write the symbol; otherwise <see langword="null"/>.
        /// </returns>
        private static StatementSyntax? GetSafeContainingStatement(
            BlockSyntax block,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (block.Parent is IfStatementSyntax ifStatement)
            {
                if (ExpressionWritesSymbol(
                        ifStatement.Condition,
                        symbol,
                        semanticModel))
                {
                    return null;
                }

                return ifStatement.Parent is BlockSyntax
                    ? ifStatement
                    : null;
            }

            if (block.Parent is ElseClauseSyntax elseClause &&
                elseClause.Parent is IfStatementSyntax elseIfStatement)
            {
                if (ExpressionWritesSymbol(
                        elseIfStatement.Condition,
                        symbol,
                        semanticModel))
                {
                    return null;
                }

                return elseIfStatement.Parent is BlockSyntax
                    ? elseIfStatement
                    : null;
            }

            if (block.Parent is BlockSyntax)
            {
                return block;
            }

            return null;
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
        /// <see langword="true"/> if the statement may write the symbol; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool StatementWritesSymbol(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? dataFlow =
                semanticModel.AnalyzeDataFlow(statement);

            return dataFlow?.Succeeded == true &&
                   dataFlow.WrittenInside.Any(
                       writtenSymbol =>
                           SymbolEqualityComparer.Default.Equals(
                               writtenSymbol,
                               symbol));
        }

        /// <summary>
        /// Determines whether an expression writes to the specified symbol.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="symbol">The symbol whose writes are detected.</param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression may write the symbol; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool ExpressionWritesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? dataFlow =
                semanticModel.AnalyzeDataFlow(expression);

            return dataFlow?.Succeeded == true &&
                   dataFlow.WrittenInside.Any(
                       writtenSymbol =>
                           SymbolEqualityComparer.Default.Equals(
                               writtenSymbol,
                               symbol));
        }

        /// <summary>
        /// Gets facts proven for a symbol when a condition evaluates to
        /// <see langword="false"/>.
        /// </summary>
        /// <param name="condition">The condition to inspect.</param>
        /// <param name="symbol">The symbol whose value facts are evaluated.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>The facts proven by the false condition result.</returns>
        private static ExceptionFlowValueFacts GetFactsProvenWhenConditionIsFalse(
            ExpressionSyntax condition,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr &&
                logicalOr.IsKind(SyntaxKind.LogicalOrExpression))
            {
                ExceptionFlowValueFacts leftFacts =
                    GetFactsProvenWhenConditionIsFalse(
                        logicalOr.Left,
                        symbol,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetFactsProvenWhenConditionIsFalse(
                        logicalOr.Right,
                        symbol,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (IsSymbolComparedEqualToNull(
                    unwrappedCondition,
                    symbol,
                    semanticModel) ||
                IsSymbolMatchedAgainstNullPattern(
                    unwrappedCondition,
                    symbol,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.NonNull;
            }

            return GetStringFactsProvenWhenConditionIsFalse(
                unwrappedCondition,
                symbol,
                semanticModel);
        }

        /// <summary>
        /// Gets string facts proven when a supported <see cref="string"/> validation
        /// method returns <see langword="false"/>.
        /// </summary>
        /// <param name="condition">The condition to inspect.</param>
        /// <param name="symbol">The symbol passed to the validation method.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>The proven string facts.</returns>
        private static ExceptionFlowValueFacts
            GetStringFactsProvenWhenConditionIsFalse(
                ExpressionSyntax condition,
                ISymbol symbol,
                SemanticModel semanticModel)
        {
            if (condition is not InvocationExpressionSyntax invocation ||
                invocation.ArgumentList.Arguments.Count != 1)
            {
                return ExceptionFlowValueFacts.None;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return ExceptionFlowValueFacts.None;
            }

            IMethodSymbol originalMethod =
                methodSymbol.OriginalDefinition;

            if (!originalMethod.IsStatic ||
                originalMethod.ContainingType.SpecialType !=
                SpecialType.System_String ||
                !ExpressionReferencesSymbol(
                    invocation.ArgumentList.Arguments[0].Expression,
                    symbol,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            if (originalMethod.Name == nameof(string.IsNullOrWhiteSpace))
            {
                return (
                    ExceptionFlowValueFacts.NonNull |
                    ExceptionFlowValueFacts.NonEmptyString |
                    ExceptionFlowValueFacts.NonWhiteSpaceString)
                    .Normalize();
            }

            if (originalMethod.Name == nameof(string.IsNullOrEmpty))
            {
                return (
                    ExceptionFlowValueFacts.NonNull |
                    ExceptionFlowValueFacts.NonEmptyString)
                    .Normalize();
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether an expression compares the specified symbol to
        /// <see langword="null"/> using the equality operator.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="symbol">The expected symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression is an equality comparison between
        /// the symbol and <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSymbolComparedEqualToNull(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (expression is not BinaryExpressionSyntax comparison ||
                !comparison.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            if (comparison.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesSymbol(
                    comparison.Right,
                    symbol,
                    semanticModel);
            }

            if (comparison.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return ExpressionReferencesSymbol(
                    comparison.Left,
                    symbol,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression matches the specified symbol against the
        /// constant <see langword="null"/> pattern.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="symbol">The expected symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression has the form
        /// <c>symbol is null</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSymbolMatchedAgainstNullPattern(
            ExpressionSyntax expression,
            ISymbol symbol,
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

            return ExpressionReferencesSymbol(
                isPatternExpression.Expression,
                symbol,
                semanticModel);
        }

        /// <summary>
        /// Determines whether an expression resolves to the specified symbol.
        /// </summary>
        /// <param name="expression">The expression to resolve.</param>
        /// <param name="symbol">The expected symbol.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// <see langword="true"/> if the expression references the specified symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ExpressionReferencesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            return symbolInfo.Symbol != null &&
                   SymbolEqualityComparer.Default.Equals(
                       symbolInfo.Symbol,
                       symbol);
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
