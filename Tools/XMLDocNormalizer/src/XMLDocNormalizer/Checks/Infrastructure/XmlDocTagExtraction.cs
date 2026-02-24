using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        /// <param name="element">The XML element (e.g. &lt;param&gt; or &lt;typeparam&gt;).</param>
        /// <returns>The name value if present; otherwise <see langword="null"/>.</returns>
        private static string? TryGetNameAttributeValue(XmlElementSyntax element)
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
        /// Extracts all named documentation tags with the given XML local name.
        /// Tags without a name attribute are ignored because well-formedness handles those cases.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name ("param").</param>
        /// <returns>A list of named documentation tags.</returns>
        internal static List<NamedDocTag> GetNamedTags(DocumentationCommentTriviaSyntax doc, string xmlTagName)
        {
            List<NamedDocTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string localName = element.StartTag.Name.LocalName.Text;
                if (!string.Equals(localName, xmlTagName, StringComparison.Ordinal))
                {
                    continue;
                }

                string? name = TryGetNameAttributeValue(element);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                tags.Add(new NamedDocTag(name, element));
            }

            return tags;
        }
    }
}
