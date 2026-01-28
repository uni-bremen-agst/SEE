using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects generic type parameter documentation smells:
    /// <list type="bullet">
    /// <item><description>DOC410: Missing <typeparam> documentation for an existing type parameter.</description></item>
    /// <item><description>DOC420: <typeparam> documentation exists but its description is empty.</description></item>
    /// <item><description>DOC430: <typeparam> references an unknown type parameter name.</description></item>
    /// <item><description>DOC450: Duplicate <typeparam> tags exist for the same type parameter name.</description></item>
    /// </list>
    /// </summary>
    internal static class XmlDocTypeParamDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC410/DOC420/DOC430/DOC450.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindTypeParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(n =>
                        n is MethodDeclarationSyntax ||
                        n is LocalFunctionStatementSyntax ||
                        n is DelegateDeclarationSyntax ||
                        n is TypeDeclarationSyntax);

            foreach (SyntaxNode declaration in declarations)
            {
                TypeParameterListSyntax? typeParams = TryGetTypeParameterList(declaration);
                if (typeParams == null)
                {
                    continue;
                }

                if (typeParams.Parameters.Count == 0)
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = TryGetDocComment(declaration);
                if (doc == null)
                {
                    // Missing overall documentation is handled by DOC100/basic detector.
                    continue;
                }

                List<string> declaredTypeParamNames = GetTypeParameterNames(typeParams);

                List<TypeParamTag> typeParamTags = GetTypeParamTags(doc);

                Dictionary<string, List<TypeParamTag>> tagsByName = GroupByName(typeParamTags);

                // DOC450: duplicates
                foreach ((string name, List<TypeParamTag> tags) in tagsByName)
                {
                    if (tags.Count <= 1)
                    {
                        continue;
                    }

                    for (int i = 1; i < tags.Count; i++)
                    {
                        TypeParamTag tag = tags[i];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "typeparam",
                            XmlDocSmells.DuplicateTypeParamTag,
                            tag.Element.SpanStart,
                            snippet: GetSnippet(tag.Element),
                            name));
                    }
                }

                // DOC420: empty description
                foreach (TypeParamTag tag in typeParamTags)
                {
                    if (string.IsNullOrWhiteSpace(tag.Name))
                    {
                        continue;
                    }

                    if (!HasMeaningfulContent(tag.Element))
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "typeparam",
                            XmlDocSmells.EmptyTypeParamDescription,
                            tag.Element.SpanStart,
                            snippet: GetSnippet(tag.Element),
                            tag.Name));
                    }
                }

                // DOC410: missing <typeparam> for an existing type parameter
                foreach (TypeParameterSyntax declared in typeParams.Parameters)
                {
                    string name = declared.Identifier.ValueText;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (tagsByName.ContainsKey(name))
                    {
                        continue;
                    }

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "typeparam",
                        XmlDocSmells.MissingTypeParamTag,
                        declared.Identifier.SpanStart,
                        snippet: declared.ToString(),
                        name));
                }

                // DOC430: unknown <typeparam name="...">
                foreach ((string documentedName, List<TypeParamTag> tags) in tagsByName)
                {
                    if (declaredTypeParamNames.Contains(documentedName))
                    {
                        continue;
                    }

                    TypeParamTag first = tags[0];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "typeparam",
                        XmlDocSmells.UnknownTypeParamTag,
                        first.Element.SpanStart,
                        snippet: GetSnippet(first.Element),
                        documentedName));
                }
            }

            return findings;
        }

        /// <summary>
        /// Tries to retrieve the <see cref="TypeParameterListSyntax"/> for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <returns>The type parameter list if present; otherwise <see langword="null"/>.</returns>
        private static TypeParameterListSyntax? TryGetTypeParameterList(SyntaxNode declaration)
        {
            if (declaration is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.TypeParameterList;
            }

            if (declaration is LocalFunctionStatementSyntax localFunctionDecl)
            {
                return localFunctionDecl.TypeParameterList;
            }

            if (declaration is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.TypeParameterList;
            }

            if (declaration is TypeDeclarationSyntax typeDecl)
            {
                return typeDecl.TypeParameterList;
            }

            return null;
        }

        /// <summary>
        /// Tries to extract the XML documentation trivia attached to the given declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node to inspect.</param>
        /// <returns>
        /// The <see cref="DocumentationCommentTriviaSyntax"/> if a documentation comment is present;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static DocumentationCommentTriviaSyntax? TryGetDocComment(SyntaxNode declaration)
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
        /// Extracts all &lt;typeparam&gt; tags (with their resolved <c>name</c> attribute) from the documentation comment.
        /// Tags without a <c>name</c> attribute are ignored here because DOC400 (well-formedness) handles that case.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <returns>A list of extracted type parameter tags.</returns>
        private static List<TypeParamTag> GetTypeParamTags(DocumentationCommentTriviaSyntax doc)
        {
            List<TypeParamTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string localName = element.StartTag.Name.LocalName.Text;
                if (!string.Equals(localName, "typeparam", StringComparison.Ordinal))
                {
                    continue;
                }

                string? name = TryGetNameAttributeValue(element);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                tags.Add(new TypeParamTag(name, element));
            }

            return tags;
        }

        /// <summary>
        /// Groups extracted type parameter tags by their <c>name</c> attribute.
        /// </summary>
        /// <param name="tags">The tags to group.</param>
        /// <returns>A dictionary mapping type parameter name to its tag occurrences.</returns>
        private static Dictionary<string, List<TypeParamTag>> GroupByName(List<TypeParamTag> tags)
        {
            Dictionary<string, List<TypeParamTag>> grouped = new(StringComparer.Ordinal);

            foreach (TypeParamTag tag in tags)
            {
                if (!grouped.TryGetValue(tag.Name, out List<TypeParamTag>? list))
                {
                    list = new List<TypeParamTag>();
                    grouped.Add(tag.Name, list);
                }

                list.Add(tag);
            }

            return grouped;
        }

        /// <summary>
        /// Collects all declared generic type parameter names from the given type parameter list.
        /// </summary>
        /// <param name="typeParams">The declared type parameter list.</param>
        /// <returns>A list of declared type parameter names.</returns>
        private static List<string> GetTypeParameterNames(TypeParameterListSyntax typeParams)
        {
            List<string> names = new();

            foreach (TypeParameterSyntax typeParam in typeParams.Parameters)
            {
                string name = typeParam.Identifier.ValueText;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        /// <summary>
        /// Extracts the <c>name</c> attribute value from a &lt;typeparam&gt; element.
        /// </summary>
        /// <param name="element">The type parameter element.</param>
        /// <returns>The attribute value if present; otherwise <see langword="null"/>.</returns>
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
        /// Determines whether the given &lt;typeparam&gt; element contains meaningful description content.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains non-whitespace text or any non-text XML node;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasMeaningfulContent(XmlElementSyntax element)
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

                // Any non-text node counts as meaningful content (e.g. <see/>, <typeparamref/>, nested tags).
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a short, single-line snippet for a syntax node suitable for console output.
        /// </summary>
        /// <param name="node">The node to create a snippet for.</param>
        /// <returns>A single-line snippet, truncated to a reasonable maximum length.</returns>
        private static string GetSnippet(SyntaxNode node)
        {
            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            return snippet;
        }

        /// <summary>
        /// Represents an extracted &lt;typeparam name="..."&gt; element and its resolved type parameter name.
        /// </summary>
        private readonly struct TypeParamTag
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TypeParamTag"/> struct.
            /// </summary>
            /// <param name="name">The type parameter name from the <c>name</c> attribute.</param>
            /// <param name="element">The corresponding XML element syntax node.</param>
            public TypeParamTag(string name, XmlElementSyntax element)
            {
                Name = name;
                Element = element;
            }

            /// <summary>
            /// Gets the type parameter name extracted from the <c>name</c> attribute.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the XML element syntax node for the &lt;typeparam&gt; element.
            /// </summary>
            public XmlElementSyntax Element { get; }
        }
    }
}
