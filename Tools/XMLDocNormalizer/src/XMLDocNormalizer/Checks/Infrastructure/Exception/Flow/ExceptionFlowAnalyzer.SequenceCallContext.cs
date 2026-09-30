using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains propagation and validation of sequence-element facts across
    /// callable boundaries.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an argument expression is proven to produce only
        /// non-null sequence elements.
        /// </summary>
        /// <param name="expression">
        /// The supplied argument expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the sequence elements are proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreSequenceElementsProvenNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            HashSet<ISymbol> inspectedValueSources =
                new(SymbolEqualityComparer.Default);

            return AreSequenceElementsProvenNonNull(
                expression,
                semanticModel,
                callerContext,
                inspectedValueSources);
        }

        /// <summary>
        /// Determines whether an argument expression is proven to produce only
        /// non-null sequence elements while preserving the active value-source
        /// recursion guard.
        /// </summary>
        /// <param name="expression">The supplied argument expression.</param>
        /// <param name="semanticModel">The semantic model of the call site.</param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the sequence elements are proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreSequenceElementsProvenNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            Conversion conversion =
                semanticModel.GetConversion(expression);

            if (conversion.IsUserDefined)
            {
                return false;
            }

            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol
                && callerContext.GetParameterFacts(parameterSymbol)
                    .ContainsAll(ExceptionFlowValueFacts.NonNullElements)
                && ExceptionFlowSequenceContentPreservationFactsProvider.IsSequenceParameterFactStillCurrentAtUse(
                    unwrappedExpression,
                    parameterSymbol,
                    semanticModel))
            {
                return true;
            }

            ISymbol? sequenceSymbol =
                symbolInfo.Symbol;

            if (sequenceSymbol is ILocalSymbol localSymbol)
            {
                if (IsDictionaryTryGetValueOutSequenceProvenNonNullElements(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }

                if (IsLocalListWithRangeAddsProvenToExcludeNullElements(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        callerContext,
                        inspectedValueSources))
                {
                    return true;
                }
            }

            if ((sequenceSymbol is ILocalSymbol
                    || sequenceSymbol is IParameterSymbol)
                && IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper(
                    unwrappedExpression,
                    sequenceSymbol,
                    semanticModel))
            {
                return true;
            }

            return IsSequenceExpressionProvenToExcludeNullElements(
                unwrappedExpression,
                semanticModel,
                callerContext,
                inspectedValueSources);
        }





    }
}
