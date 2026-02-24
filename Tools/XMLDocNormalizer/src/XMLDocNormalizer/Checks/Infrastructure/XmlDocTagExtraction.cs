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
    }
}
