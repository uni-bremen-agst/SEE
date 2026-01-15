using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Helper methods for XML documentation checks.
    /// </summary>
    internal static class XmlDocHelpers
    {
        /// <summary>
        /// Checks whether an XML element has a specific attribute of a given type and name.
        /// </summary>
        /// <typeparam name="T">The type of XML attribute syntax (e.g., XmlNameAttributeSyntax, XmlCrefAttributeSyntax).</typeparam>
        /// <param name="element">The XML element to check.</param>
        /// <param name="attributeName">The name of the attribute to look for.</param>
        /// <returns>True if the element has the attribute; otherwise, false.</returns>
        public static bool HasAttribute<T>(XmlElementSyntax element, string attributeName)
            where T : XmlAttributeSyntax
        {
            return element.StartTag.Attributes
                .OfType<T>()
                .Any(a => a.Name.LocalName.Text == attributeName);
        }
    }
}
