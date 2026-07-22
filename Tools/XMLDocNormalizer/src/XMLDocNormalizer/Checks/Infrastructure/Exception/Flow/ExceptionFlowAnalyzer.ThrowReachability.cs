using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains reachability analysis for explicit throw statements and throw
    /// expressions.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Represents a condition value proven by exception-flow value facts.
        /// </summary>
        private enum ProvenConditionValue
        {
            /// <summary>
            /// The condition value cannot be decided safely.
            /// </summary>
            Unknown,

            /// <summary>
            /// The condition is proven false.
            /// </summary>
            False,

            /// <summary>
            /// The condition is proven true.
            /// </summary>
            True
        }

        /// <summary>
        /// Determines whether a throw statement is located in an if-branch that
        /// cannot be entered under the current call-site facts.
        /// </summary>
        /// <param name="throwStatement">The throw statement to inspect.</param>
        /// <param name="analysisRoot">The root node of the current analysis.</param>
        /// <param name="semanticModel">
        /// The semantic model used for condition analysis.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an enclosing branch is proven unreachable;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsThrowStatementProvenUnreachable(
            ThrowStatementSyntax throwStatement,
            SyntaxNode analysisRoot,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            foreach (IfStatementSyntax ifStatement
                     in throwStatement.Ancestors()
                         .OfType<IfStatementSyntax>())
            {
                if (!analysisRoot.Span.Contains(ifStatement.Span))
                {
                    break;
                }

                ProvenConditionValue conditionValue =
                    EvaluateCondition(
                        ifStatement.Condition,
                        semanticModel,
                        callContext);

                if (conditionValue == ProvenConditionValue.Unknown)
                {
                    continue;
                }

                bool isInThenBranch =
                    ifStatement.Statement.Span.Contains(
                        throwStatement.Span);

                bool isInElseBranch =
                    ifStatement.Else?.Statement.Span.Contains(
                        throwStatement.Span) == true;

                if (isInThenBranch &&
                    conditionValue == ProvenConditionValue.False)
                {
                    return true;
                }

                if (isInElseBranch &&
                    conditionValue == ProvenConditionValue.True)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a throw expression is unreachable because the
        /// alternative branch of a null-coalescing or conditional expression is
        /// proven to be selected.
        /// </summary>
        /// <param name="throwExpression">The throw expression to inspect.</param>
        /// <param name="analysisRoot">The root node of the current analysis.</param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the throw expression is proven unreachable;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsThrowExpressionProvenUnreachable(
            ThrowExpressionSyntax throwExpression,
            SyntaxNode analysisRoot,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            SyntaxNode? current = throwExpression;

            while (current != null &&
                   !ReferenceEquals(current, analysisRoot))
            {
                SyntaxNode? parent = current.Parent;

                if (parent is BinaryExpressionSyntax coalesceExpression &&
                    coalesceExpression.IsKind(
                        SyntaxKind.CoalesceExpression) &&
                    coalesceExpression.Right.Span.Contains(
                        throwExpression.Span))
                {
                    ExceptionFlowValueFacts leftFacts =
                        GetExpressionValueFacts(
                            coalesceExpression.Left,
                            semanticModel,
                            callContext);

                    if (leftFacts.ContainsAll(
                            ExceptionFlowValueFacts.NonNull))
                    {
                        return true;
                    }
                }

                if (parent
                        is ConditionalExpressionSyntax conditionalExpression)
                {
                    ProvenConditionValue conditionValue =
                        EvaluateCondition(
                            conditionalExpression.Condition,
                            semanticModel,
                            callContext);

                    if (conditionalExpression.WhenTrue.Span.Contains(
                            throwExpression.Span) &&
                        conditionValue == ProvenConditionValue.False)
                    {
                        return true;
                    }

                    if (conditionalExpression.WhenFalse.Span.Contains(
                            throwExpression.Span) &&
                        conditionValue == ProvenConditionValue.True)
                    {
                        return true;
                    }
                }

                current = parent;
            }

            return false;
        }

        /// <summary>
        /// Evaluates a supported boolean condition using proven expression facts.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateCondition(
            ExpressionSyntax condition,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            ExpressionSyntax unwrappedCondition =
                UnwrapParenthesizedExpression(condition);

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(unwrappedCondition);

            if (constantValue.HasValue &&
                constantValue.Value is bool booleanValue)
            {
                return booleanValue
                    ? ProvenConditionValue.True
                    : ProvenConditionValue.False;
            }

            if (unwrappedCondition
                    is PrefixUnaryExpressionSyntax logicalNot &&
                logicalNot.IsKind(
                    SyntaxKind.LogicalNotExpression))
            {
                return NegateConditionValue(
                    EvaluateCondition(
                        logicalNot.Operand,
                        semanticModel,
                        callContext));
            }

            if (unwrappedCondition is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.IsKind(
                        SyntaxKind.LogicalAndExpression))
                {
                    return EvaluateLogicalAnd(
                        binaryExpression,
                        semanticModel,
                        callContext);
                }

                if (binaryExpression.IsKind(
                        SyntaxKind.LogicalOrExpression))
                {
                    return EvaluateLogicalOr(
                        binaryExpression,
                        semanticModel,
                        callContext);
                }

                ProvenConditionValue nullComparison =
                    EvaluateNullComparison(
                        binaryExpression,
                        semanticModel,
                        callContext);

                if (nullComparison != ProvenConditionValue.Unknown)
                {
                    return nullComparison;
                }
            }

            ProvenConditionValue nullPattern =
                EvaluateNullPattern(
                    unwrappedCondition,
                    semanticModel,
                    callContext);

            if (nullPattern != ProvenConditionValue.Unknown)
            {
                return nullPattern;
            }

            return EvaluateStringPredicate(
                unwrappedCondition,
                semanticModel,
                callContext);
        }

        /// <summary>
        /// Evaluates a logical-and expression using proven condition values.
        /// </summary>
        /// <param name="expression">The logical-and expression to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateLogicalAnd(
            BinaryExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            ProvenConditionValue left =
                EvaluateCondition(
                    expression.Left,
                    semanticModel,
                    callContext);

            ProvenConditionValue right =
                EvaluateCondition(
                    expression.Right,
                    semanticModel,
                    callContext);

            if (left == ProvenConditionValue.False ||
                right == ProvenConditionValue.False)
            {
                return ProvenConditionValue.False;
            }

            if (left == ProvenConditionValue.True &&
                right == ProvenConditionValue.True)
            {
                return ProvenConditionValue.True;
            }

            return ProvenConditionValue.Unknown;
        }

        /// <summary>
        /// Evaluates a logical-or expression using proven condition values.
        /// </summary>
        /// <param name="expression">The logical-or expression to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateLogicalOr(
            BinaryExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            ProvenConditionValue left =
                EvaluateCondition(
                    expression.Left,
                    semanticModel,
                    callContext);

            ProvenConditionValue right =
                EvaluateCondition(
                    expression.Right,
                    semanticModel,
                    callContext);

            if (left == ProvenConditionValue.True ||
                right == ProvenConditionValue.True)
            {
                return ProvenConditionValue.True;
            }

            if (left == ProvenConditionValue.False &&
                right == ProvenConditionValue.False)
            {
                return ProvenConditionValue.False;
            }

            return ProvenConditionValue.Unknown;
        }

        /// <summary>
        /// Evaluates a comparison between an expression and
        /// <see langword="null"/>.
        /// </summary>
        /// <param name="expression">The comparison expression to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value-fact resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateNullComparison(
            BinaryExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            bool isEquals =
                expression.IsKind(
                    SyntaxKind.EqualsExpression);

            bool isNotEquals =
                expression.IsKind(
                    SyntaxKind.NotEqualsExpression);

            if (!isEquals && !isNotEquals)
            {
                return ProvenConditionValue.Unknown;
            }

            ExpressionSyntax? valueExpression = null;

            if (expression.Left.IsKind(
                    SyntaxKind.NullLiteralExpression))
            {
                valueExpression = expression.Right;
            }
            else if (expression.Right.IsKind(
                         SyntaxKind.NullLiteralExpression))
            {
                valueExpression = expression.Left;
            }

            if (valueExpression == null)
            {
                return ProvenConditionValue.Unknown;
            }

            ExceptionFlowValueFacts facts =
                GetExpressionValueFacts(
                    valueExpression,
                    semanticModel,
                    callContext);

            if (!facts.ContainsAll(
                    ExceptionFlowValueFacts.NonNull))
            {
                return ProvenConditionValue.Unknown;
            }

            return isEquals
                ? ProvenConditionValue.False
                : ProvenConditionValue.True;
        }

        /// <summary>
        /// Evaluates a supported null-pattern expression.
        /// </summary>
        /// <param name="expression">The pattern expression to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value-fact resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateNullPattern(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (expression
                    is not IsPatternExpressionSyntax isPatternExpression)
            {
                return ProvenConditionValue.Unknown;
            }

            ExceptionFlowValueFacts facts =
                GetExpressionValueFacts(
                    isPatternExpression.Expression,
                    semanticModel,
                    callContext);

            if (!facts.ContainsAll(
                    ExceptionFlowValueFacts.NonNull))
            {
                return ProvenConditionValue.Unknown;
            }

            if (isPatternExpression.Pattern
                    is ConstantPatternSyntax constantPattern &&
                constantPattern.Expression.IsKind(
                    SyntaxKind.NullLiteralExpression))
            {
                return ProvenConditionValue.False;
            }

            if (isPatternExpression.Pattern
                    is UnaryPatternSyntax unaryPattern &&
                unaryPattern.IsKind(
                    SyntaxKind.NotPattern) &&
                unaryPattern.Pattern
                    is ConstantPatternSyntax innerConstantPattern &&
                innerConstantPattern.Expression.IsKind(
                    SyntaxKind.NullLiteralExpression))
            {
                return ProvenConditionValue.True;
            }

            return ProvenConditionValue.Unknown;
        }

        /// <summary>
        /// Evaluates a supported <see cref="string"/> null or content predicate.
        /// </summary>
        /// <param name="expression">The predicate expression to evaluate.</param>
        /// <param name="semanticModel">
        /// The semantic model used for method and argument resolution.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <returns>The proven condition value.</returns>
        private static ProvenConditionValue EvaluateStringPredicate(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (expression
                    is not InvocationExpressionSyntax invocation ||
                invocation.ArgumentList.Arguments.Count != 1)
            {
                return ProvenConditionValue.Unknown;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return ProvenConditionValue.Unknown;
            }

            IMethodSymbol originalMethod =
                methodSymbol.OriginalDefinition;

            if (!originalMethod.IsStatic ||
                originalMethod.ContainingType.SpecialType !=
                SpecialType.System_String)
            {
                return ProvenConditionValue.Unknown;
            }

            ExpressionSyntax argumentExpression =
                invocation.ArgumentList.Arguments[0].Expression;

            ExceptionFlowValueFacts facts =
                GetExpressionValueFacts(
                    argumentExpression,
                    semanticModel,
                    callContext);

            if (originalMethod.Name ==
                    nameof(string.IsNullOrWhiteSpace) &&
                facts.ContainsAll(
                    ExceptionFlowValueFacts.NonWhiteSpaceString))
            {
                return ProvenConditionValue.False;
            }

            if (originalMethod.Name ==
                    nameof(string.IsNullOrEmpty) &&
                facts.ContainsAll(
                    ExceptionFlowValueFacts.NonEmptyString))
            {
                return ProvenConditionValue.False;
            }

            return ProvenConditionValue.Unknown;
        }

        /// <summary>
        /// Negates a proven condition value.
        /// </summary>
        /// <param name="value">The condition value to negate.</param>
        /// <returns>The negated condition value.</returns>
        private static ProvenConditionValue NegateConditionValue(
            ProvenConditionValue value)
        {
            return value switch
            {
                ProvenConditionValue.True =>
                    ProvenConditionValue.False,

                ProvenConditionValue.False =>
                    ProvenConditionValue.True,

                _ => ProvenConditionValue.Unknown
            };
        }
    }
}
