using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides control-flow facts established by terminating value guards.
    /// </summary>
    internal static class ExceptionFlowGuardFactsProvider
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
        internal static bool IsLocalProvenNonNullByPrecedingGuard(
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
        internal static ExceptionFlowValueFacts GetFactsProvenByPrecedingGuard(
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

                StatementSyntax? containingStatement =
                    ExceptionFlowDereferenceFactDiscovery.GetSafeContainingStatement(
                        containingBlock,
                        symbol,
                        semanticModel);

                if (containingStatement is IfStatementSyntax enclosingIfStatement
                    && ReferenceEquals(
                        enclosingIfStatement.Statement,
                        containingBlock))
                {
                    facts |= GetFactsProvenWhenConditionIsTrue(
                        enclosingIfStatement.Condition,
                        symbol,
                        semanticModel);
                }

                if (containingStatement == null)
                {
                    break;
                }

                currentStatement = containingStatement;
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
                    ExceptionFlowArgumentMapper.GetParameterIndex(
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
        /// Gets value facts established by earlier operands that had to be
        /// evaluated successfully before the current expression can be reached
        /// through short-circuit Boolean evaluation.
        /// </summary>
        /// <param name="expression">
        /// The expression whose evaluation position is inspected.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose facts are requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// The facts implied by earlier short-circuit operands.
        /// </returns>
        internal static ExceptionFlowValueFacts GetFactsProvenByEarlierShortCircuitConditions(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExceptionFlowValueFacts facts = ExceptionFlowValueFacts.None;

            foreach (BinaryExpressionSyntax binaryExpression
                     in expression.Ancestors().OfType<BinaryExpressionSyntax>())
            {
                if (!binaryExpression.Right.Span.Contains(expression.Span))
                {
                    continue;
                }

                if (binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    facts |= GetFactsProvenWhenConditionIsTrue(
                        binaryExpression.Left,
                        symbol,
                        semanticModel);

                    continue;
                }

                if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression))
                {
                    facts |= GetFactsProvenWhenConditionIsFalse(
                        binaryExpression.Left,
                        symbol,
                        semanticModel);
                }
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets facts proven for a symbol when a condition evaluates to
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="condition">
        /// The condition to inspect.
        /// </param>
        /// <param name="symbol">
        /// The symbol whose value facts are evaluated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// The facts proven by the true condition result.
        /// </returns>
        private static ExceptionFlowValueFacts GetFactsProvenWhenConditionIsTrue(
            ExpressionSyntax condition,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is BinaryExpressionSyntax logicalAnd
                && logicalAnd.IsKind(SyntaxKind.LogicalAndExpression))
            {
                ExceptionFlowValueFacts leftFacts =
                    GetFactsProvenWhenConditionIsTrue(
                        logicalAnd.Left,
                        symbol,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetFactsProvenWhenConditionIsTrue(
                        logicalAnd.Right,
                        symbol,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (unwrappedCondition is PrefixUnaryExpressionSyntax logicalNot
                && logicalNot.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return GetFactsProvenWhenConditionIsFalse(
                    logicalNot.Operand,
                    symbol,
                    semanticModel);
            }

            if (IsSymbolComparedNotEqualToNull(
                    unwrappedCondition,
                    symbol,
                    semanticModel)
                || IsSymbolMatchedAgainstNotNullPattern(
                    unwrappedCondition,
                    symbol,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.NonNull;
            }

            return ExceptionFlowValueFacts.None;
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
        internal static bool IsSymbolComparedEqualToNull(
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
        /// Determines whether an expression compares the specified symbol to
        /// <see langword="null"/> using the inequality operator.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="symbol">
        /// The expected symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is an inequality comparison
        /// between the symbol and <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSymbolComparedNotEqualToNull(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (expression is not BinaryExpressionSyntax comparison
                || !comparison.IsKind(SyntaxKind.NotEqualsExpression))
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
        internal static bool IsSymbolMatchedAgainstNullPattern(
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
        /// Determines whether an expression matches the specified symbol against
        /// the <c>not null</c> pattern.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="symbol">
        /// The expected symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression has the form
        /// <c>symbol is not null</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSymbolMatchedAgainstNotNullPattern(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (expression is not IsPatternExpressionSyntax isPatternExpression
                || isPatternExpression.Pattern is not UnaryPatternSyntax notPattern
                || !notPattern.IsKind(SyntaxKind.NotPattern)
                || notPattern.Pattern is not ConstantPatternSyntax constantPattern
                || !constantPattern.Expression.IsKind(
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
        /// Gets value facts established by the directly controlling
        /// <c>if</c> branch of a return statement.
        /// </summary>
        /// <param name="expression">
        /// The returned expression whose enclosing branch should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and invocation resolution.
        /// </param>
        /// <returns>
        /// The facts established for the returned expression by the branch
        /// condition, or <see cref="ExceptionFlowValueFacts.None"/> when the
        /// branch is unsupported.
        /// </returns>
        internal static ExceptionFlowValueFacts
            GetFactsProvenByDirectContainingReturnBranch(
                ExpressionSyntax expression,
                SemanticModel semanticModel)
        {
            if (expression.Parent is not ReturnStatementSyntax returnStatement
                || !TryGetDirectContainingIfBranch(
                    returnStatement,
                    out IfStatementSyntax? ifStatement,
                    out bool branchConditionValue)
                || ifStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            return GetStringFactsProvenForStableExpressionByCondition(
                ifStatement.Condition,
                expression,
                branchConditionValue,
                semanticModel);
        }

        /// <summary>
        /// Attempts to identify an <c>if</c> statement whose selected branch
        /// consists directly of the supplied return statement.
        /// </summary>
        /// <param name="returnStatement">
        /// The return statement to inspect.
        /// </param>
        /// <param name="ifStatement">
        /// The containing <c>if</c> statement when successful.
        /// </param>
        /// <param name="branchConditionValue">
        /// <see langword="true"/> for the true branch and
        /// <see langword="false"/> for the else branch.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported direct containing branch
        /// exists; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDirectContainingIfBranch(
            ReturnStatementSyntax returnStatement,
            out IfStatementSyntax? ifStatement,
            out bool branchConditionValue)
        {
            ifStatement = null;
            branchConditionValue = false;

            if (returnStatement.Parent is IfStatementSyntax directIf
                && ReferenceEquals(directIf.Statement, returnStatement))
            {
                ifStatement = directIf;
                branchConditionValue = true;
                return true;
            }

            if (returnStatement.Parent is ElseClauseSyntax directElse
                && directElse.Parent is IfStatementSyntax directElseIf
                && ReferenceEquals(directElse.Statement, returnStatement))
            {
                ifStatement = directElseIf;
                branchConditionValue = false;
                return true;
            }

            if (returnStatement.Parent is not BlockSyntax block
                || block.Statements.Count != 1
                || !ReferenceEquals(block.Statements[0], returnStatement))
            {
                return false;
            }

            if (block.Parent is IfStatementSyntax blockIf
                && ReferenceEquals(blockIf.Statement, block))
            {
                ifStatement = blockIf;
                branchConditionValue = true;
                return true;
            }

            if (block.Parent is ElseClauseSyntax blockElse
                && blockElse.Parent is IfStatementSyntax blockElseIf
                && ReferenceEquals(blockElse.Statement, block))
            {
                ifStatement = blockElseIf;
                branchConditionValue = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets string facts established for a stable expression by a
        /// supported boolean condition result.
        /// </summary>
        /// <param name="condition">
        /// The controlling condition.
        /// </param>
        /// <param name="valueExpression">
        /// The stable returned expression whose facts should be derived.
        /// </param>
        /// <param name="conditionValue">
        /// The condition result required to enter the return branch.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression and invocation resolution.
        /// </param>
        /// <returns>
        /// The string facts established by the condition result.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetStringFactsProvenForStableExpressionByCondition(
                ExpressionSyntax condition,
                ExpressionSyntax valueExpression,
                bool conditionValue,
                SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            if (unwrappedCondition is PrefixUnaryExpressionSyntax negation
                && negation.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return GetStringFactsProvenForStableExpressionByCondition(
                    negation.Operand,
                    valueExpression,
                    !conditionValue,
                    semanticModel);
            }

            if (unwrappedCondition is BinaryExpressionSyntax logicalAnd
                && logicalAnd.IsKind(SyntaxKind.LogicalAndExpression)
                && conditionValue)
            {
                ExceptionFlowValueFacts leftFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalAnd.Left,
                        valueExpression,
                        true,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalAnd.Right,
                        valueExpression,
                        true,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (unwrappedCondition is BinaryExpressionSyntax logicalOr
                && logicalOr.IsKind(SyntaxKind.LogicalOrExpression)
                && !conditionValue)
            {
                ExceptionFlowValueFacts leftFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalOr.Left,
                        valueExpression,
                        false,
                        semanticModel);

                ExceptionFlowValueFacts rightFacts =
                    GetStringFactsProvenForStableExpressionByCondition(
                        logicalOr.Right,
                        valueExpression,
                        false,
                        semanticModel);

                return (leftFacts | rightFacts).Normalize();
            }

            if (conditionValue
                || unwrappedCondition is not InvocationExpressionSyntax invocation
                || invocation.ArgumentList.Arguments.Count != 1
                || semanticModel.GetSymbolInfo(invocation).Symbol
                    is not IMethodSymbol methodSymbol
                || !methodSymbol.OriginalDefinition.IsStatic
                || methodSymbol.OriginalDefinition.ContainingType.SpecialType
                    != SpecialType.System_String)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax guardedExpression =
                invocation.ArgumentList.Arguments[0].Expression;

            if (!AreEquivalentStableValueExpressions(
                    guardedExpression,
                    valueExpression,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            if (methodSymbol.OriginalDefinition.Name
                == nameof(string.IsNullOrWhiteSpace))
            {
                return (
                    ExceptionFlowValueFacts.NonNull
                    | ExceptionFlowValueFacts.NonEmptyString
                    | ExceptionFlowValueFacts.NonWhiteSpaceString)
                    .Normalize();
            }

            if (methodSymbol.OriginalDefinition.Name
                == nameof(string.IsNullOrEmpty))
            {
                return (
                    ExceptionFlowValueFacts.NonNull
                    | ExceptionFlowValueFacts.NonEmptyString)
                    .Normalize();
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether two expressions represent the same stable value
        /// for the duration of a direct guarded return.
        /// </summary>
        /// <param name="left">
        /// The first expression.
        /// </param>
        /// <param name="right">
        /// The second expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for stability analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expressions are syntactically
        /// equivalent and represent supported stable values; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool AreEquivalentStableValueExpressions(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedLeft =
                UnwrapParenthesizedExpression(left);

            ExpressionSyntax unwrappedRight =
                UnwrapParenthesizedExpression(right);

            return SyntaxFactory.AreEquivalent(
                       unwrappedLeft,
                       unwrappedRight)
                && IsStableGuardValueExpression(
                    unwrappedLeft,
                    semanticModel)
                && IsStableGuardValueExpression(
                    unwrappedRight,
                    semanticModel);
        }

        /// <summary>
        /// Determines whether an expression represents a value that cannot
        /// change between a supported string-validation condition and its
        /// direct return.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for locals, non-ref parameters, and get-only
        /// auto-properties; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsStableGuardValueExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol is ILocalSymbol)
            {
                return true;
            }

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                return parameterSymbol.RefKind == RefKind.None;
            }

            if (symbolInfo.Symbol is not IPropertySymbol propertySymbol
                || propertySymbol.SetMethod != null
                || propertySymbol.DeclaringSyntaxReferences.Length != 1
                || propertySymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not PropertyDeclarationSyntax propertyDeclaration
                || propertyDeclaration.ExpressionBody != null
                || propertyDeclaration.AccessorList == null)
            {
                return false;
            }

            return propertyDeclaration.AccessorList.Accessors.Any(
                static accessor =>
                    accessor.IsKind(SyntaxKind.GetAccessorDeclaration)
                    && accessor.Body == null
                    && accessor.ExpressionBody == null);
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
