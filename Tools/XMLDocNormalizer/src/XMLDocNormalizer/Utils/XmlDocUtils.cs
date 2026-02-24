using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides helper methods for working with XML documentation comments.
    /// </summary>
    internal static class XmlDocUtils
    {
        /// <summary>
        /// Tries to retrieve the documentation comment trivia attached to the specified syntax node.
        /// </summary>
        /// <param name="node">
        /// The syntax node that may have leading XML documentation trivia.
        /// </param>
        /// <returns>
        /// The <see cref="DocumentationCommentTriviaSyntax"/> instance if a documentation
        /// comment is found; otherwise <c>null</c>.
        /// </returns>
        public static DocumentationCommentTriviaSyntax? TryGetDocComment(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    SyntaxNode? structure = trivia.GetStructure();

                    if (structure is DocumentationCommentTriviaSyntax documentation)
                    {
                        return documentation;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given XML element contains meaningful content.
        /// Meaningful content is any non-whitespace text or any nested XML node (like <see/>, <para>, etc.).
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns>True if the element contains meaningful content; otherwise false.</returns>
        public static bool HasMeaningfulContent(XmlElementSyntax element)
        {
            foreach (XmlNodeSyntax node in element.Content)
            {
                if (node is XmlTextSyntax text)
                {
                    foreach (SyntaxToken token in text.TextTokens)
                    {
                        if (!string.IsNullOrWhiteSpace(token.ValueText))
                        {
                            return true;
                        }
                    }
                    continue;
                }

                // Any other XML node counts as content
                return true;
            }

            return false;
        }
    }
}
