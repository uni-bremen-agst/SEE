using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides intrinsic value facts for scalar constants and string
    /// expressions.
    /// </summary>
    internal static class ExceptionFlowPrimitiveValueFactsProvider
    {
        /// <summary>
        /// Determines whether an expression is a built-in C# string
        /// concatenation rather than a user-defined addition operator.
        /// </summary>
        /// <param name="expression">The binary expression to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for operation resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression uses the built-in string
        /// concatenation semantics; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsBuiltInStringConcatenation(
            BinaryExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            if (!expression.IsKind(SyntaxKind.AddExpression)
                || semanticModel.GetOperation(expression)
                    is not IBinaryOperation binaryOperation)
            {
                return false;
            }

            return binaryOperation.OperatorKind == BinaryOperatorKind.Add
                && binaryOperation.OperatorMethod == null
                && binaryOperation.Type?.SpecialType == SpecialType.System_String;
        }

        /// <summary>
        /// Determines whether an interpolated-string expression is converted
        /// to <see cref="string"/>.
        /// </summary>
        /// <param name="expression">
        /// The interpolated-string expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the effective expression type is
        /// <see cref="string"/>; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsStringExpression(
            InterpolatedStringExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            TypeInfo typeInfo = semanticModel.GetTypeInfo(expression);
            ITypeSymbol? effectiveType = typeInfo.ConvertedType ?? typeInfo.Type;

            return effectiveType?.SpecialType == SpecialType.System_String;
        }

        /// <summary>
        /// Gets value facts guaranteed by the fixed text segments of an
        /// interpolated string.
        /// </summary>
        /// <param name="expression">
        /// The interpolated-string expression to inspect.
        /// </param>
        /// <returns>
        /// Facts guaranteed independently of the values produced by the
        /// interpolation expressions.
        /// </returns>
        internal static ExceptionFlowValueFacts GetInterpolatedStringValueFacts(
            InterpolatedStringExpressionSyntax expression)
        {
            ExceptionFlowValueFacts facts = ExceptionFlowValueFacts.NonNull;

            foreach (InterpolatedStringContentSyntax content in expression.Contents)
            {
                if (content is not InterpolatedStringTextSyntax text)
                {
                    continue;
                }

                string textValue = text.TextToken.ValueText;

                if (textValue.Length > 0)
                {
                    facts |= ExceptionFlowValueFacts.NonEmptyString;
                }

                if (!string.IsNullOrWhiteSpace(textValue))
                {
                    facts |= ExceptionFlowValueFacts.NonWhiteSpaceString;
                }
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets value facts for a compile-time constant or explicit default
        /// value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        /// <returns>The facts proven by the constant value.</returns>
        internal static ExceptionFlowValueFacts GetConstantValueFacts(object? value)
        {
            if (value == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts facts = ExceptionFlowValueFacts.NonNull;

            if (value is string stringValue)
            {
                if (stringValue.Length > 0)
                {
                    facts |= ExceptionFlowValueFacts.NonEmptyString;
                }

                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    facts |= ExceptionFlowValueFacts.NonWhiteSpaceString;
                }
            }

            return facts.Normalize();
        }
    }
}
