using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides reusable syntax helpers for Roslyn-based XML documentation analysis.
    /// </summary>
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
        /// <typeparam name="T">The type of XML attribute syntax to search for.</typeparam>
        /// <param name="element">The XML element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns><see langword="true"/> if a matching attribute exists; otherwise <see langword="false"/>.</returns>
        internal static bool HasAttribute<T>(XmlElementSyntax element, string localName)
            where T : XmlAttributeSyntax
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(localName);

            return GetAttribute<T>(element, localName) != null;
        }

        /// <summary>
        /// Determines whether the specified XML empty element contains an attribute
        /// with the given local name.
        /// </summary>
        /// <typeparam name="TAttribute">The expected attribute syntax type.</typeparam>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns><see langword="true"/> if a matching attribute exists; otherwise <see langword="false"/>.</returns>
        internal static bool HasAttribute<TAttribute>(
            XmlEmptyElementSyntax element,
            string localName)
            where TAttribute : XmlAttributeSyntax
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(localName);

            return GetAttribute<TAttribute>(element, localName) != null;
        }

        /// <summary>
        /// Gets an XML attribute of the specified type and local name from an XML element.
        /// </summary>
        /// <typeparam name="T">The expected attribute syntax type.</typeparam>
        /// <param name="element">The XML element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The matching attribute if found; otherwise <see langword="null"/>.</returns>
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
        /// Gets an XML attribute of the specified type and local name from an XML empty element.
        /// </summary>
        /// <typeparam name="T">The expected attribute syntax type.</typeparam>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The matching attribute if found; otherwise <see langword="null"/>.</returns>
        internal static T? GetAttribute<T>(
            XmlEmptyElementSyntax element,
            string localName)
            where T : XmlAttributeSyntax
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(localName);

            foreach (XmlAttributeSyntax attribute in element.Attributes)
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
        /// Gets all attributes of an XML element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>The element attributes.</returns>
        internal static IEnumerable<XmlAttributeSyntax> GetAttributes(XmlElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.StartTag.Attributes;
        }

        /// <summary>
        /// Gets all attributes of an XML empty element.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>The element attributes.</returns>
        internal static IEnumerable<XmlAttributeSyntax> GetAttributes(XmlEmptyElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.Attributes;
        }

        /// <summary>
        /// Gets the local tag name of an XML element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>The local tag name.</returns>
        internal static string GetLocalName(XmlElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.StartTag.Name.LocalName.Text;
        }

        /// <summary>
        /// Gets the local tag name of an XML empty element.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>The local tag name.</returns>
        internal static string GetLocalName(XmlEmptyElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return element.Name.LocalName.Text;
        }

        /// <summary>
        /// Gets the unquoted value of the specified XML attribute on an XML element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The unquoted attribute value if present; otherwise <see langword="null"/>.</returns>
        internal static string? GetAttributeValue(XmlElementSyntax element, string localName)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(localName);

            XmlAttributeSyntax? attribute = GetAttribute<XmlAttributeSyntax>(element, localName);
            return GetAttributeValue(attribute);
        }

        /// <summary>
        /// Gets the unquoted value of the specified XML attribute on an XML empty element.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The unquoted attribute value if present; otherwise <see langword="null"/>.</returns>
        internal static string? GetAttributeValue(XmlEmptyElementSyntax element, string localName)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(localName);

            XmlAttributeSyntax? attribute = GetAttribute<XmlAttributeSyntax>(element, localName);
            return GetAttributeValue(attribute);
        }

        /// <summary>
        /// Determines whether the specified XML element contains non-whitespace content.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains any non-whitespace content;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasNonEmptyContent(XmlElementSyntax element)
        {
            ArgumentNullException.ThrowIfNull(element);

            foreach (XmlNodeSyntax node in element.Content)
            {
                if (node is XmlTextSyntax textNode)
                {
                    foreach (SyntaxToken token in textNode.TextTokens)
                    {
                        if (!string.IsNullOrWhiteSpace(token.ValueText))
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

        /// <summary>
        /// Tries to get the executable body node for a member (block body or expression-bodied form).
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <param name="bodyNode">The extracted body node if present.</param>
        /// <returns><see langword="true"/> if a body exists; otherwise <see langword="false"/>.</returns>
        internal static bool TryGetMemberBody(MemberDeclarationSyntax member, out SyntaxNode? bodyNode)
        {
            if (member is MethodDeclarationSyntax methodDecl)
            {
                bodyNode = (SyntaxNode?)methodDecl.Body ?? methodDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is ConstructorDeclarationSyntax ctorDecl)
            {
                bodyNode = (SyntaxNode?)ctorDecl.Body ?? ctorDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is DestructorDeclarationSyntax dtorDecl)
            {
                bodyNode = (SyntaxNode?)dtorDecl.Body ?? dtorDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is OperatorDeclarationSyntax opDecl)
            {
                bodyNode = (SyntaxNode?)opDecl.Body ?? opDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is ConversionOperatorDeclarationSyntax convDecl)
            {
                bodyNode = (SyntaxNode?)convDecl.Body ?? convDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is PropertyDeclarationSyntax propertyDecl)
            {
                bodyNode = propertyDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            if (member is IndexerDeclarationSyntax indexerDecl)
            {
                bodyNode = indexerDecl.ExpressionBody?.Expression;
                return bodyNode != null;
            }

            bodyNode = null;
            return false;
        }

        /// <summary>
        /// Determines whether the member has an executable body and can therefore throw exceptions directly.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if the member has an executable body; otherwise <see langword="false"/>.</returns>
        internal static bool HasExecutableBody(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax method)
            {
                return method.Body != null
                    || method.ExpressionBody != null;
            }

            if (member is ConstructorDeclarationSyntax constructor)
            {
                return constructor.Body != null
                    || constructor.ExpressionBody != null;
            }

            if (member is DestructorDeclarationSyntax destructor)
            {
                return destructor.Body != null
                    || destructor.ExpressionBody != null;
            }

            if (member is OperatorDeclarationSyntax operatorDeclaration)
            {
                return operatorDeclaration.Body != null
                    || operatorDeclaration.ExpressionBody != null;
            }

            if (member is ConversionOperatorDeclarationSyntax conversionOperator)
            {
                return conversionOperator.Body != null
                    || conversionOperator.ExpressionBody != null;
            }

            if (member is PropertyDeclarationSyntax property)
            {
                if (property.ExpressionBody != null)
                {
                    return true;
                }

                return property.AccessorList?.Accessors.Any(
                    static accessor => accessor.Body != null || accessor.ExpressionBody != null) == true;
            }

            if (member is IndexerDeclarationSyntax indexer)
            {
                if (indexer.ExpressionBody != null)
                {
                    return true;
                }

                return indexer.AccessorList?.Accessors.Any(
                    static accessor => accessor.Body != null || accessor.ExpressionBody != null) == true;
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                return eventDeclaration.AccessorList?.Accessors.Any(
                    static accessor => accessor.Body != null || accessor.ExpressionBody != null) == true;
            }

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

        /// <summary>
        /// Determines whether the specified member is declared as abstract.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the member is declared with the <c>abstract</c> modifier;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsAbstractMember(MemberDeclarationSyntax member)
        {
            if (member is BaseMethodDeclarationSyntax methodBase)
            {
                return methodBase.Modifiers.Any(static m => m.IsKind(SyntaxKind.AbstractKeyword));
            }

            if (member is PropertyDeclarationSyntax property)
            {
                return property.Modifiers.Any(static m => m.IsKind(SyntaxKind.AbstractKeyword));
            }

            if (member is IndexerDeclarationSyntax indexer)
            {
                return indexer.Modifiers.Any(static m => m.IsKind(SyntaxKind.AbstractKeyword));
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                return eventDeclaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.AbstractKeyword));
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified member is declared as extern.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the member is declared with the <c>extern</c> modifier;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsExternMember(MemberDeclarationSyntax member)
        {
            if (member is BaseMethodDeclarationSyntax methodBase)
            {
                return methodBase.Modifiers.Any(static m => m.IsKind(SyntaxKind.ExternKeyword));
            }

            if (member is PropertyDeclarationSyntax property)
            {
                return property.Modifiers.Any(static m => m.IsKind(SyntaxKind.ExternKeyword));
            }

            if (member is IndexerDeclarationSyntax indexer)
            {
                return indexer.Modifiers.Any(static m => m.IsKind(SyntaxKind.ExternKeyword));
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                return eventDeclaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.ExternKeyword));
            }

            return false;
        }

        /// <summary>
        /// Gets the unquoted value of an XML attribute.
        /// </summary>
        /// <param name="attribute">The attribute to inspect.</param>
        /// <returns>The unquoted attribute value if present; otherwise <see langword="null"/>.</returns>
        private static string? GetAttributeValue(XmlAttributeSyntax? attribute)
        {
            if (attribute == null)
            {
                return null;
            }

            if (attribute is XmlTextAttributeSyntax textAttribute)
            {
                return string.Concat(textAttribute.TextTokens.Select(static token => token.ValueText));
            }

            if (attribute is XmlCrefAttributeSyntax crefAttribute)
            {
                return crefAttribute.Cref?.ToString();
            }

            return null;
        }
    }
}
