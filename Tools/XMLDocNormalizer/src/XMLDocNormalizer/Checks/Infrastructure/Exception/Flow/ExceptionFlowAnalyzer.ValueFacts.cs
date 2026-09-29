using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is CastExpressionSyntax castExpression)
            {
                ExceptionFlowValueFacts castFacts =
                    GetExpressionValueFacts(
                        castExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                ExceptionFlowValueFacts enumFacts =
                    GetDefinedEnumValueFacts(
                        unwrappedExpression,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                return (castFacts | enumFacts).Normalize();
            }

            if (unwrappedExpression is CheckedExpressionSyntax checkedExpression)
            {
                return GetExpressionValueFacts(
                    checkedExpression.Expression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);
            }

            if (unwrappedExpression is ConditionalExpressionSyntax conditionalExpression)
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

            ExceptionFlowValueFacts sourcePositionFacts =
                ExceptionFlowSourcePositionValueFactsProvider.GetOneBasedSourcePositionValueFacts(
                    unwrappedExpression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);

            ExceptionFlowValueFacts enumValueFacts =
                GetDefinedEnumValueFacts(
                    unwrappedExpression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);

            if (unwrappedExpression is InterpolatedStringExpressionSyntax interpolatedString
                && ExceptionFlowPrimitiveValueFactsProvider.IsStringExpression(
                    interpolatedString,
                    semanticModel))
            {
                return ExceptionFlowPrimitiveValueFactsProvider
                    .GetInterpolatedStringValueFacts(interpolatedString);
            }

            if (unwrappedExpression is BinaryExpressionSyntax binaryExpression
                && ExceptionFlowPrimitiveValueFactsProvider
                    .IsBuiltInStringConcatenation(binaryExpression, semanticModel))
            {
                return GetStringConcatenationValueFacts(
                    binaryExpression,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);
            }

            if (unwrappedExpression is InvocationExpressionSyntax invocationExpression)
            {
                if (TryGetSourceInvocationReturnValueFacts(
                        invocationExpression,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers,
                        out ExceptionFlowValueFacts invocationFacts))
                {
                    return invocationFacts.Normalize();
                }

                ExceptionFlowValueFacts frameworkInvocationFacts =
                    GetKnownFrameworkInvocationValueFacts(
                        invocationExpression,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                if (frameworkInvocationFacts != ExceptionFlowValueFacts.None)
                {
                    return (
                        sourcePositionFacts
                        | frameworkInvocationFacts)
                        .Normalize();
                }
            }

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(
                    unwrappedExpression);

            if (constantValue.HasValue)
            {
                return (
                    ExceptionFlowPrimitiveValueFactsProvider
                        .GetConstantValueFacts(constantValue.Value)
                    | sourcePositionFacts
                    | enumValueFacts)
                    .Normalize();
            }

            ExceptionFlowValueFacts facts =
                sourcePositionFacts | enumValueFacts;

            HashSet<ISymbol> inspectedReturnSymbols =
                new(
                    inspectedImmutableMembers,
                    SymbolEqualityComparer.Default);

            if (IsDefinitelyNonNull(
                    unwrappedExpression,
                    semanticModel,
                    callContext,
                    inspectedReturnSymbols))
            {
                facts |= ExceptionFlowValueFacts.NonNull;
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
                        ExceptionFlowGuardFactsProvider.GetFactsProvenByPrecedingGuard(
                            unwrappedExpression,
                            parameterSymbol,
                            semanticModel);

                    facts |=
                        ExceptionFlowDereferenceFactDiscovery.GetFactsProvenByPrecedingSuccessfulDereference(
                            unwrappedExpression,
                            parameterSymbol,
                            semanticModel);

                    facts |=
                        ExceptionFlowGuardFactsProvider
                            .GetFactsProvenByEarlierShortCircuitConditions(
                            unwrappedExpression,
                            parameterSymbol,
                            semanticModel);
                    break;

                case ILocalSymbol localSymbol:
                    facts |= ExceptionFlowGuardFactsProvider
                        .GetFactsProvenByPrecedingGuard(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel);

                    facts |= ExceptionFlowDereferenceFactDiscovery.GetFactsProvenByPrecedingSuccessfulDereference(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel);

                    facts |= ExceptionFlowGuardFactsProvider
                        .GetFactsProvenByEarlierShortCircuitConditions(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel);

                    if (inspectedImmutableMembers.Add(localSymbol))
                    {
                        try
                        {
                            if (ExceptionFlowLocalInitializerFactsProvider.TryGetStraightLineCurrentLocalInitializerExpression(
                                    unwrappedExpression,
                                    localSymbol,
                                    semanticModel,
                                    out ExpressionSyntax? initializer)
                                && initializer != null)
                            {
                                facts |=
                                    GetExpressionValueFacts(
                                        initializer,
                                        semanticModel,
                                        callContext,
                                        inspectedImmutableMembers);
                            }
                        }
                        finally
                        {
                            inspectedImmutableMembers.Remove(
                                localSymbol);
                        }
                    }

                    break;

                case IFieldSymbol fieldSymbol:
                    facts |=
                        GetImmutableMemberValueFacts(
                            fieldSymbol,
                            semanticModel,
                            inspectedImmutableMembers);
                    break;

                case IPropertySymbol propertySymbol:
                    facts |= ExceptionFlowKnownPropertyValueFactsProvider
                        .GetKnownFrameworkPropertyValueFacts(propertySymbol);

                    facts |= GetImmutableMemberValueFacts(
                        propertySymbol,
                        semanticModel,
                        inspectedImmutableMembers);

                    facts |= GetFactsProvenByCurrentLocalStablePropertyInitializer(
                        unwrappedExpression,
                        propertySymbol,
                        semanticModel,
                        callContext,
                        inspectedImmutableMembers);

                    facts |=
                        GetDictionaryEntryValueFacts(
                            unwrappedExpression,
                            propertySymbol,
                            semanticModel,
                            callContext);

                    facts |= ExceptionFlowDereferenceFactDiscovery.GetFactsProvenByPrecedingSuccessfulStablePropertyDereference(
                        unwrappedExpression,
                        propertySymbol,
                        semanticModel);

                    if (ExceptionFlowDereferenceFactDiscovery.TryGetStableCallContextPropertyReceiverParameter(
                            unwrappedExpression,
                            propertySymbol,
                            semanticModel,
                            out IParameterSymbol? receiverParameter)
                        && receiverParameter != null
                        && callContext.IsParameterMemberKnownNonNull(
                            receiverParameter,
                            propertySymbol)
                        && ExceptionFlowDereferenceFactDiscovery.IsParameterValueStillCurrentSinceEntry(
                            unwrappedExpression,
                            receiverParameter,
                            semanticModel))
                    {
                        facts |= ExceptionFlowValueFacts.NonNull;
                    }

                    break;
            }

            return facts.Normalize();
        }

        /// <summary>
        /// Gets value facts guaranteed by a built-in string concatenation.
        /// </summary>
        /// <param name="expression">
        /// The built-in string concatenation expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operand analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being analyzed.
        /// </param>
        /// <returns>
        /// Facts guaranteed for the concatenated string.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetStringConcatenationValueFacts(
                BinaryExpressionSyntax expression,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callContext,
                HashSet<ISymbol> inspectedImmutableMembers)
        {
            ExceptionFlowValueFacts leftFacts =
                GetExpressionValueFacts(
                    expression.Left,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);

            ExceptionFlowValueFacts rightFacts =
                GetExpressionValueFacts(
                    expression.Right,
                    semanticModel,
                    callContext,
                    inspectedImmutableMembers);

            ExceptionFlowValueFacts facts =
                ExceptionFlowValueFacts.NonNull;

            if (leftFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonEmptyString) ||
                rightFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonEmptyString))
            {
                facts |=
                    ExceptionFlowValueFacts.NonEmptyString;
            }

            if (leftFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonWhiteSpaceString) ||
                rightFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonWhiteSpaceString))
            {
                facts |=
                    ExceptionFlowValueFacts.NonWhiteSpaceString;
            }

            return facts.Normalize();
        }

    }
}
