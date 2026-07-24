using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains helpers for creating structured exception-flow paths.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
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
        private static ExceptionFlowPathStep CreatePathStep(
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
        private static ExceptionFlowPath CreateTerminalPath(
            ExceptionFlowPathStepKind kind,
            ISymbol symbol,
            SyntaxNode sourceNode)
        {
            return new ExceptionFlowPath(
                CreatePathStep(
                    kind,
                    symbol,
                    sourceNode));
        }
    }
}
