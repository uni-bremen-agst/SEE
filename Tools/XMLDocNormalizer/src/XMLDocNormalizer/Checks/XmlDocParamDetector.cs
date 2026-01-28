using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects parameter documentation smells:
    /// - DOC310: Missing <param> documentation for an existing parameter.
    /// - DOC320: <param> documentation exists but its description is empty.
    /// - DOC330: <param> references an unknown parameter name.
    /// - DOC350: Duplicate <param> tags exist for the same parameter name.
    /// </summary>
    internal static class XmlDocParamDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC310/DOC320/DOC330/DOC350.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindParamSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> declarations =
                root.DescendantNodes()
                    .Where(n =>
                        n is MethodDeclarationSyntax ||
                        n is ConstructorDeclarationSyntax ||
                        n is DelegateDeclarationSyntax ||
                        n is IndexerDeclarationSyntax ||
                        n is OperatorDeclarationSyntax ||
                        n is ConversionOperatorDeclarationSyntax ||
                        n is LocalFunctionStatementSyntax);

            foreach (SyntaxNode declaration in declarations)
            {
                if (!TryGetParameters(declaration, out SeparatedSyntaxList<ParameterSyntax> parameters))
                {
                    continue;
                }

                if (parameters.Count == 0)
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = TryGetDocComment(declaration);
                if (doc == null)
                {
                    // Missing overall documentation is handled by DOC100 (basic detector).
                    continue;
                }

                List<string> parameterNames = GetParameterNames(parameters);

                List<ParamTag> paramTags = GetParamTags(doc);

                Dictionary<string, List<ParamTag>> paramTagsByName =
                    GroupByName(paramTags);

                // DOC350: duplicates (<param> duplicated '{0}')
                foreach ((string name, List<ParamTag> tags) in paramTagsByName)
                {
                    if (tags.Count <= 1)
                    {
                        continue;
                    }

                    // Report at the second and subsequent occurrences.
                    for (int i = 1; i < tags.Count; i++)
                    {
                        ParamTag tag = tags[i];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "param",
                            XmlDocSmells.DuplicateParamTag,
                            tag.Element.SpanStart,
                            snippet: GetSnippet(tag.Element),
                            name));
                    }
                }

                // DOC320: empty description
                foreach (ParamTag tag in paramTags)
                {
                    if (string.IsNullOrWhiteSpace(tag.Name))
                    {
                        // Well-formed detector should handle missing name attribute.
                        continue;
                    }

                    if (!HasMeaningfulContent(tag.Element))
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "param",
                            XmlDocSmells.EmptyParamDescription,
                            tag.Element.SpanStart,
                            snippet: GetSnippet(tag.Element),
                            tag.Name));
                    }
                }

                // DOC310: missing <param> for an existing parameter
                foreach (ParameterSyntax parameter in parameters)
                {
                    string paramName = parameter.Identifier.ValueText;
                    if (string.IsNullOrWhiteSpace(paramName))
                    {
                        continue;
                    }

                    if (paramTagsByName.ContainsKey(paramName))
                    {
                        continue;
                    }

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "param",
                        XmlDocSmells.MissingParamTag,
                        parameter.Identifier.SpanStart,
                        snippet: parameter.ToString(),
                        paramName));
                }

                // DOC330: unknown <param name="...">
                foreach ((string documentedName, List<ParamTag> tags) in paramTagsByName)
                {
                    if (parameterNames.Contains(documentedName))
                    {
                        continue;
                    }

                    ParamTag first = tags[0];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "param",
                        XmlDocSmells.UnknownParamTag,
                        first.Element.SpanStart,
                        snippet: GetSnippet(first.Element),
                        documentedName));
                }
            }

            return findings;
        }

        /// <summary>
        /// Tries to get the parameters for a supported declaration node.
        /// </summary>
        /// <param name="declaration">The declaration node.</param>
        /// <param name="parameters">The extracted parameters.</param>
        /// <returns><see langword="true"/> if parameters could be extracted; otherwise <see langword="false"/>.</returns>
        private static bool TryGetParameters(SyntaxNode declaration, out SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            if (declaration is MethodDeclarationSyntax methodDecl)
            {
                parameters = methodDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConstructorDeclarationSyntax ctorDecl)
            {
                parameters = ctorDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is DelegateDeclarationSyntax delegateDecl)
            {
                parameters = delegateDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is IndexerDeclarationSyntax indexerDecl)
            {
                parameters = indexerDecl.ParameterList.Parameters; // BracketedParameterListSyntax
                return true;
            }

            if (declaration is OperatorDeclarationSyntax operatorDecl)
            {
                parameters = operatorDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is ConversionOperatorDeclarationSyntax conversionDecl)
            {
                parameters = conversionDecl.ParameterList.Parameters;
                return true;
            }

            if (declaration is LocalFunctionStatementSyntax localFunctionDecl)
            {
                parameters = localFunctionDecl.ParameterList.Parameters;
                return true;
            }

            parameters = default;
            return false;
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
        /// Extracts all &lt;param&gt; tags (with their resolved name attribute) from the documentation comment.
        /// Tags without a name attribute are ignored because the well-formed detector is responsible for them.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <returns>A list of param tags.</returns>
        private static List<ParamTag> GetParamTags(DocumentationCommentTriviaSyntax doc)
        {
            List<ParamTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string localName = element.StartTag.Name.LocalName.Text;
                if (!string.Equals(localName, "param", StringComparison.Ordinal))
                {
                    continue;
                }

                string? name = TryGetNameAttributeValue(element);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                tags.Add(new ParamTag(name, element));
            }

            return tags;
        }

        /// <summary>
        /// Groups param tags by parameter name.
        /// </summary>
        /// <param name="tags">The param tags to group.</param>
        /// <returns>A dictionary mapping parameter name to the list of corresponding tags.</returns>
        private static Dictionary<string, List<ParamTag>> GroupByName(List<ParamTag> tags)
        {
            Dictionary<string, List<ParamTag>> grouped = new(StringComparer.Ordinal);

            foreach (ParamTag tag in tags)
            {
                if (!grouped.TryGetValue(tag.Name, out List<ParamTag>? list))
                {
                    list = new List<ParamTag>();
                    grouped.Add(tag.Name, list);
                }

                list.Add(tag);
            }

            return grouped;
        }

        /// <summary>
        /// Collects parameter names from a parameter list.
        /// </summary>
        /// <param name="parameters">The parameters to inspect.</param>
        /// <returns>A list of parameter names.</returns>
        private static List<string> GetParameterNames(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            List<string> names = new();

            foreach (ParameterSyntax parameter in parameters)
            {
                string name = parameter.Identifier.ValueText;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        /// <summary>
        /// Extracts the <c>name</c> attribute value from a &lt;param&gt; element.
        /// </summary>
        /// <param name="element">The param element.</param>
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
        /// Determines whether the given &lt;param&gt; element contains meaningful description content.
        /// </summary>
        /// <param name="paramElement">The param element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains non-whitespace text or any non-text XML node;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasMeaningfulContent(XmlElementSyntax paramElement)
        {
            foreach (XmlNodeSyntax node in paramElement.Content)
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

                // Any non-text node counts as content (e.g. <see/>, <paramref/>, nested tags).
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

        private readonly struct ParamTag
        {
            public ParamTag(string name, XmlElementSyntax element)
            {
                Name = name;
                Element = element;
            }

            public string Name { get; }

            public XmlElementSyntax Element { get; }
        }
    }
}
