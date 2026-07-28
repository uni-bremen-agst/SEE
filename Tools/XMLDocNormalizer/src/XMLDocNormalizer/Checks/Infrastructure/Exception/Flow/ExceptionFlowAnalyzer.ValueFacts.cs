using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning used during exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets the value facts that are proven for an expression at its
        /// current control-flow position.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// The facts proven for the expression.
        /// </returns>
        private static ExceptionFlowValueFacts GetExpressionValueFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            HashSet<ISymbol> inspectedImmutableMembers =
                new(SymbolEqualityComparer.Default);

            return GetExpressionValueFacts(
                expression,
                semanticModel,
                callContext,
                inspectedImmutableMembers);
        }

        /// <summary>
        /// Gets value facts proven for an expression while preventing
        /// recursive immutable-member analysis.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being analyzed.
        /// </param>
        /// <returns>
        /// The facts proven for the expression.
        /// </returns>
        private static ExceptionFlowValueFacts GetExpressionValueFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedImmutableMembers)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(
                    expression);

            if (unwrappedExpression
                is CastExpressionSyntax castExpression)
            {
                return GetExpressionValueFacts(
                    castExpression.Expression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);
            }

            if (unwrappedExpression
                is CheckedExpressionSyntax checkedExpression)
            {
                return GetExpressionValueFacts(
                    checkedExpression.Expression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);
            }

            if (unwrappedExpression
                is ConditionalExpressionSyntax conditionalExpression)
            {
                ExceptionFlowValueFacts trueFacts =
                    GetExpressionValueFacts(
                        conditionalExpression.WhenTrue,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                ExceptionFlowValueFacts falseFacts =
                    GetExpressionValueFacts(
                        conditionalExpression.WhenFalse,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                return (trueFacts & falseFacts).Normalize();
            }

            if (unwrappedExpression
                    is InterpolatedStringExpressionSyntax
                        interpolatedString &&
                IsStringExpression(
                    interpolatedString,
                    semanticModel))
            {
                return GetInterpolatedStringValueFacts(
                    interpolatedString);
            }

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(
                    unwrappedExpression);

            if (constantValue.HasValue)
            {
                return GetConstantValueFacts(
                    constantValue.Value);
            }

            ExceptionFlowValueFacts facts =
                ExceptionFlowValueFacts.None;

            if (IsDefinitelyNonNull(
                    unwrappedExpression,
                    semanticModel,
                    callContext))
            {
                facts |=
                    ExceptionFlowValueFacts.NonNull;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            switch (symbolInfo.Symbol)
            {
                case IParameterSymbol parameterSymbol:
                    facts |=
                        callContext.GetParameterFacts(
                            parameterSymbol);

                    facts |=
                        GetFactsProvenByPrecedingGuard(
                            unwrappedExpression,
                            parameterSymbol,
                            semanticModel);
                    break;

                case ILocalSymbol localSymbol:
                    facts |=
                        GetFactsProvenByPrecedingGuard(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel);
                    break;

                case IFieldSymbol fieldSymbol:
                    facts |=
                        GetImmutableMemberValueFacts(
                            fieldSymbol,
                            semanticModel,
                            inspectedImmutableMembers);
                    break;

                case IPropertySymbol propertySymbol:
                    facts |=
                        GetImmutableMemberValueFacts(
                            propertySymbol,
                            semanticModel,
                            inspectedImmutableMembers);
                    break;
            }

            return facts.Normalize();
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
        private static bool IsStringExpression(
            InterpolatedStringExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    expression);

            ITypeSymbol? effectiveType =
                typeInfo.ConvertedType ??
                typeInfo.Type;

            return effectiveType?.SpecialType ==
                   SpecialType.System_String;
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
        private static ExceptionFlowValueFacts
            GetInterpolatedStringValueFacts(
                InterpolatedStringExpressionSyntax expression)
        {
            ExceptionFlowValueFacts facts =
                ExceptionFlowValueFacts.NonNull;

            foreach (InterpolatedStringContentSyntax content
                     in expression.Contents)
            {
                if (content
                    is not InterpolatedStringTextSyntax text)
                {
                    continue;
                }

                string textValue =
                    text.TextToken.ValueText;

                if (textValue.Length > 0)
                {
                    facts |=
                        ExceptionFlowValueFacts.NonEmptyString;
                }

                if (!string.IsNullOrWhiteSpace(
                        textValue))
                {
                    facts |=
                        ExceptionFlowValueFacts
                            .NonWhiteSpaceString;
                }
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets value facts for a compile-time constant or explicit default
        /// value.
        /// </summary>
        /// <param name="value">
        /// The constant value.
        /// </param>
        /// <returns>
        /// The facts proven by the constant value.
        /// </returns>
        private static ExceptionFlowValueFacts GetConstantValueFacts(
            object? value)
        {
            if (value == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts facts =
                ExceptionFlowValueFacts.NonNull;

            if (value is string stringValue)
            {
                if (stringValue.Length > 0)
                {
                    facts |=
                        ExceptionFlowValueFacts.NonEmptyString;
                }

                if (!string.IsNullOrWhiteSpace(
                        stringValue))
                {
                    facts |=
                        ExceptionFlowValueFacts
                            .NonWhiteSpaceString;
                }
            }

            return facts.Normalize();
        }
    }
}
