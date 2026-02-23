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
    }
}
