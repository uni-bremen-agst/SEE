using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Records unresolved callable symbols in exception-flow results.
    /// </summary>
    internal static class ExceptionFlowUncertaintyRecorder
    {
        /// <summary>
        /// Adds the stable display name of one unresolved symbol.
        /// </summary>
        /// <param name="result">The result receiving the uncertainty.</param>
        /// <param name="symbol">The unresolved callable symbol.</param>
        internal static void AddSymbol(
            ExceptionFlowAnalysisResult result,
            ISymbol symbol)
        {
            string display =
                symbol.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(display))
            {
                display = symbol.Name;
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                result.UncertainTargets.Add(display);
            }
        }
    }
}
