using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains non-null reasoning used during exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an expression is proven to evaluate to a
        /// non-null value without relying only on nullable reference-type
        /// annotations.
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
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            HashSet<ISymbol> inspectedReturnSymbols =
                new(SymbolEqualityComparer.Default);

            return ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(
                expression,
                semanticModel,
                callContext,
                inspectedReturnSymbols);
        }






    }
}
