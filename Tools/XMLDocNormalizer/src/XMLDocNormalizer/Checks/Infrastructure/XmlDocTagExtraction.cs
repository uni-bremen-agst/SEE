using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides shared extraction helpers for XML documentation analysis.
    /// </summary>
    internal static class XmlDocTagExtraction
    {
        /// <summary>
        /// Extracts the <c>name</c> attribute value from a name-based XML documentation element.
        /// </summary>
        /// <param name="element">
        /// The XML element (e.g. &lt;param&gt; or &lt;typeparam&gt;).
        /// </param>
        /// <returns>
        /// The name value if present; otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="element"/> is <c>null</c>.
        /// </exception>
        internal static string? TryGetNameAttributeValue(XmlElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);

            XmlNameAttributeSyntax? nameAttribute =
                SyntaxUtils.GetAttribute<XmlNameAttributeSyntax>(element, "name");

            if (nameAttribute == null)
            {
                return null;
            }

            IdentifierNameSyntax? identifier = nameAttribute.Identifier;
            if (identifier == null)
            {
                return null;
            }

            return identifier.Identifier.ValueText;
        }

        /// <summary>
        /// Tries to extract the value of the <c>cref</c> attribute from an XML documentation element.
        /// </summary>
        /// <param name="element">
        /// The XML documentation element.
        /// </param>
        /// <param name="cref">
        /// When this method returns <c>true</c>, contains the extracted cref value.
        /// Otherwise, contains <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a <c>cref</c> attribute exists and a value could be extracted; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="element"/> is <c>null</c>.
        /// </exception>
        public static bool TryGetCrefAttributeValue(XmlElementSyntax element, out string? cref)
        {
            ArgumentNullException.ThrowIfNull(element);

            cref = null;

            XmlCrefAttributeSyntax? crefAttribute =
                SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(element, "cref");

            if (crefAttribute == null)
            {
                return false;
            }

            CrefSyntax? crefSyntax = crefAttribute.Cref;
            if (crefSyntax == null)
            {
                return false;
            }

            string value = crefSyntax.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            cref = value;
            return true;
        }

        /// <summary>
        /// Extracts XML documentation elements for a given tag name and associates them with an extracted attribute value.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <param name="tagLocalName">The XML tag local name (e.g. "param", "typeparam", "exception").</param>
        /// <param name="attributeValueExtractor">
        /// A function that extracts the relevant attribute value from an element (may return null).
        /// </param>
        /// <returns>
        /// A list of extracted tags containing the element and the extracted attribute value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="doc"/>, <paramref name="tagLocalName"/> or <paramref name="attributeValueExtractor"/> is <c>null</c>.
        /// </exception>
        public static List<ExtractedXmlDocTag> ExtractTags(
            DocumentationCommentTriviaSyntax doc,
            string tagLocalName,
            Func<XmlElementSyntax, string?> attributeValueExtractor)
        {
            ArgumentNullException.ThrowIfNull(doc);

            ArgumentNullException.ThrowIfNull(tagLocalName);

            ArgumentNullException.ThrowIfNull(attributeValueExtractor);

            List<ExtractedXmlDocTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                XmlDocElementQuery.ElementsByName(doc, tagLocalName);

            foreach (XmlElementSyntax element in elements)
            {
                string? rawValue = attributeValueExtractor(element);
                tags.Add(new ExtractedXmlDocTag(element, rawValue));
            }

            return tags;
        }
    }
}
