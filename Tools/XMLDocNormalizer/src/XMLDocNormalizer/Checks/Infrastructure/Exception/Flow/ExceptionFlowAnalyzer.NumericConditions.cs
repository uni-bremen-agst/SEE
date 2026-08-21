using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains condition reasoning based on proven numeric value facts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Evaluates a comparison involving a value proven to be a positive
        /// 32-bit signed integer and an integer constant.
        /// </summary>
        /// <param name="expression">
        /// The comparison expression to evaluate.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve constants and value facts.
        /// </param>
        /// <param name="callContext">
        /// The current callable value facts.
        /// </param>
        /// <returns>
        /// The proven truth value of the comparison, or
        /// <see cref="ProvenConditionValue.Unknown"/> when the available facts
        /// are insufficient.
        /// </returns>
        private static ProvenConditionValue EvaluatePositiveInt32Comparison(
            BinaryExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (!IsSupportedPositiveInt32Comparison(expression.Kind()))
            {
                return ProvenConditionValue.Unknown;
            }

            if (TryGetInt32Constant(expression.Right, semanticModel, out int rightConstant))
            {
                ExceptionFlowValueFacts leftFacts = GetExpressionValueFacts(
                    expression.Left,
                    semanticModel,
                    callContext);

                if (leftFacts.ContainsAll(ExceptionFlowValueFacts.PositiveInt32))
                {
                    return EvaluatePositiveInt32AgainstConstant(
                        expression.Kind(),
                        rightConstant,
                        valueOnLeft: true);
                }
            }

            if (TryGetInt32Constant(expression.Left, semanticModel, out int leftConstant))
            {
                ExceptionFlowValueFacts rightFacts = GetExpressionValueFacts(
                    expression.Right,
                    semanticModel,
                    callContext);

                if (rightFacts.ContainsAll(ExceptionFlowValueFacts.PositiveInt32))
                {
                    return EvaluatePositiveInt32AgainstConstant(
                        expression.Kind(),
                        leftConstant,
                        valueOnLeft: false);
                }
            }

            return ProvenConditionValue.Unknown;
        }

        /// <summary>
        /// Evaluates one supported comparison using the invariant that the
        /// non-constant operand is greater than or equal to one.
        /// </summary>
        /// <param name="comparisonKind">
        /// The syntax kind of the comparison expression.
        /// </param>
        /// <param name="constant">
        /// The constant integer operand.
        /// </param>
        /// <param name="valueOnLeft">
        /// Whether the proven positive integer is the left operand.
        /// </param>
        /// <returns>
        /// The proven truth value of the comparison, or
        /// <see cref="ProvenConditionValue.Unknown"/> when positivity alone
        /// does not determine the result.
        /// </returns>
        private static ProvenConditionValue EvaluatePositiveInt32AgainstConstant(
            SyntaxKind comparisonKind,
            int constant,
            bool valueOnLeft)
        {
            if (valueOnLeft)
            {
                return comparisonKind switch
                {
                    SyntaxKind.LessThanExpression when constant <= 1 =>
                        ProvenConditionValue.False,

                    SyntaxKind.LessThanOrEqualExpression when constant < 1 =>
                        ProvenConditionValue.False,

                    SyntaxKind.GreaterThanExpression when constant < 1 =>
                        ProvenConditionValue.True,

                    SyntaxKind.GreaterThanOrEqualExpression when constant <= 1 =>
                        ProvenConditionValue.True,

                    SyntaxKind.EqualsExpression when constant < 1 =>
                        ProvenConditionValue.False,

                    SyntaxKind.NotEqualsExpression when constant < 1 =>
                        ProvenConditionValue.True,

                    _ => ProvenConditionValue.Unknown
                };
            }

            return comparisonKind switch
            {
                SyntaxKind.LessThanExpression when constant < 1 =>
                    ProvenConditionValue.True,

                SyntaxKind.LessThanOrEqualExpression when constant <= 1 =>
                    ProvenConditionValue.True,

                SyntaxKind.GreaterThanExpression when constant <= 1 =>
                    ProvenConditionValue.False,

                SyntaxKind.GreaterThanOrEqualExpression when constant < 1 =>
                    ProvenConditionValue.False,

                SyntaxKind.EqualsExpression when constant < 1 =>
                    ProvenConditionValue.False,

                SyntaxKind.NotEqualsExpression when constant < 1 =>
                    ProvenConditionValue.True,

                _ => ProvenConditionValue.Unknown
            };
        }

        /// <summary>
        /// Determines whether a syntax kind represents an integer comparison
        /// supported by positive-value reasoning.
        /// </summary>
        /// <param name="kind">
        /// The syntax kind to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the comparison can be evaluated by this
        /// analysis; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedPositiveInt32Comparison(SyntaxKind kind)
        {
            return kind is SyntaxKind.LessThanExpression
                or SyntaxKind.LessThanOrEqualExpression
                or SyntaxKind.GreaterThanExpression
                or SyntaxKind.GreaterThanOrEqualExpression
                or SyntaxKind.EqualsExpression
                or SyntaxKind.NotEqualsExpression;
        }

        /// <summary>
        /// Attempts to resolve an expression to a compile-time
        /// <see cref="int"/> constant.
        /// </summary>
        /// <param name="expression">
        /// The expression to evaluate.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to retrieve the constant value.
        /// </param>
        /// <param name="value">
        /// The resolved integer value when the method succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression resolves to an
        /// <see cref="int"/> constant; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetInt32Constant(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out int value)
        {
            Optional<object?> constantValue = semanticModel.GetConstantValue(expression);

            if (constantValue.HasValue && constantValue.Value is int intValue)
            {
                value = intValue;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
