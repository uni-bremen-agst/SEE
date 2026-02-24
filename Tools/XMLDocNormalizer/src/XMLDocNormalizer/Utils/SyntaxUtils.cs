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
        internal static bool HasAttribute<T>(XmlElementSyntax element, string localName)
            where T : XmlAttributeSyntax
        {
            ArgumentNullException.ThrowIfNull(element);

            ArgumentNullException.ThrowIfNull(localName);

            return GetAttribute<T>(element, localName) != null;
        }

        /// <summary>
        /// Gets an XML attribute of the specified type and local name from an XML element.
        /// </summary>
        /// <typeparam name="T">
        /// The expected attribute syntax type (e.g. <see cref="XmlCrefAttributeSyntax"/>).
        /// </typeparam>
        /// <param name="element">
        /// The XML element.
        /// </param>
        /// <param name="localName">
        /// The local attribute name (e.g. "cref", "name").
        /// </param>
        /// <returns>
        /// The matching attribute if found; otherwise <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="element"/> or <paramref name="localName"/> is null.
        /// </exception>
        internal static T? GetAttribute<T>(
            XmlElementSyntax element,
            string localName)
            where T : XmlAttributeSyntax
        {
            ArgumentNullException.ThrowIfNull(element);

            ArgumentNullException.ThrowIfNull(localName);

            foreach (XmlAttributeSyntax attribute in element.StartTag.Attributes)
            {
                if (attribute is not T typedAttribute)
                {
                    continue;
                }

                string name = typedAttribute.Name.LocalName.Text;

                if (string.Equals(name, localName, StringComparison.Ordinal))
                {
                    return typedAttribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to get the executable body node for a member (block body or expression-bodied).
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <param name="bodyNode">The extracted body node if present.</param>
        /// <returns>True if a body exists; otherwise false.</returns>
        internal static bool TryGetMemberBody(MemberDeclarationSyntax member, out SyntaxNode? bodyNode)
        {
            if (member is MethodDeclarationSyntax methodDecl)
            {
                bodyNode = (SyntaxNode?)methodDecl.Body ?? methodDecl.ExpressionBody;
                return bodyNode != null;
            }

            if (member is ConstructorDeclarationSyntax ctorDecl)
            {
                bodyNode = (SyntaxNode?)ctorDecl.Body ?? ctorDecl.ExpressionBody;
                return bodyNode != null;
            }

            if (member is DestructorDeclarationSyntax dtorDecl)
            {
                bodyNode = (SyntaxNode?)dtorDecl.Body ?? dtorDecl.ExpressionBody;
                return bodyNode != null;
            }

            if (member is OperatorDeclarationSyntax opDecl)
            {
                bodyNode = (SyntaxNode?)opDecl.Body ?? opDecl.ExpressionBody;
                return bodyNode != null;
            }

            if (member is ConversionOperatorDeclarationSyntax convDecl)
            {
                bodyNode = (SyntaxNode?)convDecl.Body ?? convDecl.ExpressionBody;
                return bodyNode != null;
            }

            bodyNode = null;
            return false;
        }

        /// <summary>
        /// Determines whether the given body contains a rethrow statement (<c>throw;</c>).
        /// </summary>
        /// <param name="bodyNode">The body node to inspect.</param>
        /// <param name="anchorPosition">The anchor position of the rethrow if found.</param>
        /// <returns><see langword="true"/> if a rethrow was found; otherwise <see langword="false"/>.</returns>
        internal static bool ContainsRethrow(SyntaxNode bodyNode, out int anchorPosition)
        {
            foreach (ThrowStatementSyntax throwStmt in bodyNode.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                if (throwStmt.Expression == null)
                {
                    anchorPosition = throwStmt.ThrowKeyword.SpanStart;
                    return true;
                }
            }

            anchorPosition = bodyNode.SpanStart;
            return false;
        }
    }
}