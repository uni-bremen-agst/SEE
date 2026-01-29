using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    internal static class SyntaxUtils
    {
        /// <summary>
        /// Creates a short, single-line snippet for a syntax node that is suitable for console output.
        /// </summary>
        /// <param name="node">The node to create a snippet for.</param>
        /// <returns>A single-line snippet, truncated to a reasonable maximum length.</returns>
        internal static string GetSnippet(SyntaxNode node)
        {
            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            return snippet;
        }

        /// <summary>
        /// Checks whether an XML element has a specific attribute of a given type and name.
        /// </summary>
        /// <typeparam name="T">The type of XML attribute syntax (e.g., XmlNameAttributeSyntax, XmlCrefAttributeSyntax).</typeparam>
        /// <param name="element">The XML element to check.</param>
        /// <param name="attributeName">The name of the attribute to look for.</param>
        /// <returns>True if the element has the attribute; otherwise, false.</returns>
        internal static bool HasAttribute<T>(XmlElementSyntax element, string attributeName)
            where T : XmlAttributeSyntax
        {
            return element.StartTag.Attributes
                .OfType<T>()
                .Any(a => a.Name.LocalName.Text == attributeName);
        }
    }
}