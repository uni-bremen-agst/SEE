using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Inspects inheritdoc source symbols for useful XML documentation.
    /// </summary>
    internal static class InheritdocDocumentationInspector
    {
        /// <summary>
        /// Determines whether the specified source symbol provides useful XML documentation.
        /// </summary>
        /// <param name="sourceSymbol">The inheritdoc source symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the symbol provides non-empty XML documentation;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasUsefulDocumentation(ISymbol sourceSymbol)
        {
            ArgumentNullException.ThrowIfNull(sourceSymbol);

            string? xml = sourceSymbol.GetDocumentationCommentXml();
            return !string.IsNullOrWhiteSpace(xml);
        }
    }
}