using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides shared extraction helpers for XML documentation analysis.
    /// </summary>
    internal static class XmlDocTagExtraction
    {
        /// <summary>
        /// Tries to extract the XML documentation trivia attached to the given declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <returns>
        /// The <see cref="DocumentationCommentTriviaSyntax"/> if a documentation comment is present;
        /// otherwise <see langword="null"/>.
        /// </returns>
        public static DocumentationCommentTriviaSyntax? TryGetDocComment(SyntaxNode declaration)
        {
            SyntaxTriviaList leadingTrivia = declaration.GetLeadingTrivia();

            foreach (SyntaxTrivia trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    SyntaxNode? structure = trivia.GetStructure();
                    DocumentationCommentTriviaSyntax? doc = structure as DocumentationCommentTriviaSyntax;
                    if (doc != null)
                    {
                        return doc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the <c>name</c> attribute value from a name-based XML documentation element.
        /// </summary>
        /// <param name="element">The XML element (e.g. &lt;param&gt; or &lt;typeparam&gt;).</param>
        /// <returns>The name value if present; otherwise <see langword="null"/>.</returns>
        public static string? TryGetNameAttributeValue(XmlElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.StartTag.Attributes)
            {
                if (attribute is XmlNameAttributeSyntax nameAttribute)
                {
                    return nameAttribute.Identifier?.Identifier.ValueText;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given XML element contains meaningful content.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains non-whitespace text or any non-text XML node;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool HasMeaningfulContent(XmlElementSyntax element)
        {
            foreach (XmlNodeSyntax node in element.Content)
            {
                if (node is XmlTextSyntax text)
                {
                    foreach (SyntaxToken token in text.TextTokens)
                    {
                        string value = token.ValueText;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return true;
                        }
                    }

                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
