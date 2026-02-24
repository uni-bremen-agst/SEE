using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides helper methods for querying XML documentation elements
    /// within a <see cref="DocumentationCommentTriviaSyntax"/>.
    /// </summary>
    internal static class XmlDocElementQuery
    {
        /// <summary>
        /// Enumerates all XML elements within the given documentation comment.
        /// </summary>
        /// <param name="doc">
        /// The documentation comment trivia.
        /// </param>
        /// <returns>
        /// All <see cref="XmlElementSyntax"/> nodes contained in <paramref name="doc"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="doc"/> is null.
        /// </exception>
        public static IEnumerable<XmlElementSyntax> Elements(DocumentationCommentTriviaSyntax doc)
        {
            ArgumentNullException.ThrowIfNull(doc);

            return doc.DescendantNodes().OfType<XmlElementSyntax>();
        }

        /// <summary>
        /// Enumerates all XML elements with the specified local name.
        /// </summary>
        /// <param name="doc">
        /// The documentation comment trivia.
        /// </param>
        /// <param name="localName">
        /// The XML element local name (e.g. "summary", "returns", "param").
        /// </param>
        /// <returns>
        /// All <see cref="XmlElementSyntax"/> nodes whose start tag matches <paramref name="localName"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="doc"/> or <paramref name="localName"/> is null.
        /// </exception>
        public static IEnumerable<XmlElementSyntax> ElementsByName(
            DocumentationCommentTriviaSyntax doc,
            string localName)
        {
            ArgumentNullException.ThrowIfNull(doc);

            ArgumentNullException.ThrowIfNull(localName);

            return Elements(doc).Where(
                element => string.Equals(
                    element.StartTag.Name.LocalName.Text,
                    localName,
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets the first XML element with the specified local name, if present.
        /// </summary>
        /// <param name="doc">
        /// The documentation comment trivia.
        /// </param>
        /// <param name="localName">
        /// The XML element local name (e.g. "summary", "returns", "remarks").
        /// </param>
        /// <returns>
        /// The first matching <see cref="XmlElementSyntax"/>, or null if none exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="doc"/> or <paramref name="localName"/> is null.
        /// </exception>
        public static XmlElementSyntax? FirstByName(
            DocumentationCommentTriviaSyntax doc,
            string localName)
        {
            ArgumentNullException.ThrowIfNull(doc);

            ArgumentNullException.ThrowIfNull(localName);

            return ElementsByName(doc, localName).FirstOrDefault();
        }
    }
}