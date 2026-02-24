using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private static string? TryGetNameAttributeValue(XmlElementSyntax element)
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
    }
}
