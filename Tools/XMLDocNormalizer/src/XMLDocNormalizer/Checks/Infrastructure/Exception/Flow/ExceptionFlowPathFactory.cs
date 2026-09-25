using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Creates structured source-level exception-flow paths.
    /// </summary>
    /// <remarks>
    /// This stateless Roslyn-bound component owns the projection from symbols
    /// and syntax locations to the neutral path model. It is safe for
    /// concurrent use.
    /// </remarks>
    internal static class ExceptionFlowPathFactory
    {
        /// <summary>
        /// Creates one source-level exception-flow path step.
        /// </summary>
        /// <param name="kind">The role of the step in the path.</param>
        /// <param name="symbol">
        /// The referenced symbol or exception type.
        /// </param>
        /// <param name="sourceNode">
        /// The source node whose position should be recorded.
        /// </param>
        /// <returns>The created path step.</returns>
        internal static ExceptionFlowPathStep CreateStep(
            ExceptionFlowPathStepKind kind,
            ISymbol symbol,
            SyntaxNode sourceNode)
        {
            FileLinePositionSpan lineSpan =
                sourceNode.GetLocation().GetLineSpan();

            LinePosition startPosition =
                lineSpan.StartLinePosition;

            string? filePath =
                string.IsNullOrWhiteSpace(lineSpan.Path)
                    ? null
                    : lineSpan.Path;

            string symbolName = symbol.ToDisplayString(
                SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(symbolName))
            {
                symbolName = $"<{symbol.Kind}>";
            }

            return new ExceptionFlowPathStep(
                kind,
                symbolName,
                filePath,
                startPosition.Line + 1,
                startPosition.Character + 1);
        }

        /// <summary>
        /// Creates a single-step path ending at an exception source.
        /// </summary>
        /// <param name="kind">The terminal step kind.</param>
        /// <param name="symbol">
        /// The terminal symbol or exception type.
        /// </param>
        /// <param name="sourceNode">
        /// The source node whose position should be recorded.
        /// </param>
        /// <returns>The created terminal exception-flow path.</returns>
        internal static ExceptionFlowPath CreateTerminal(
            ExceptionFlowPathStepKind kind,
            ISymbol symbol,
            SyntaxNode sourceNode)
        {
            return new ExceptionFlowPath(
                CreateStep(
                    kind,
                    symbol,
                    sourceNode));
        }
    }
}
