using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Configuration;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects basic documentation smells:
    /// - DOC100: Missing documentation comment.
    /// - DOC200: Missing summary-tag.
    /// - DOC210: Empty summary-tag.
    /// </summary>
    internal static class XmlDocBasicDetector
    {
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath)
        {
            return FindBasicSmells(tree, filePath, new XmlDocOptions());
        }
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC100/DOC200/DOC210.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath, XmlDocOptions options)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!options.CheckEnumMembers && member is EnumMemberDeclarationSyntax)
                {
                    continue;
                }
                DocumentationCommentTriviaSyntax? doc = TryGetDocComment(member);

                if (doc == null)
                {
                    // DOC100 – Missing documentation comment.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "documentation",
                        XmlDocSmells.MissingDocumentation,
                        GetAnchorPosition(member)));
                    continue;
                }

                // If fields are allowed to omit <summary>, skip DOC200/DOC210 for fields.
                if (!options.RequireSummaryForFields
                    && (member is FieldDeclarationSyntax || member is EventFieldDeclarationSyntax))
                {
                    continue;
                }

                XmlElementSyntax? summaryElement = FindFirstElement(doc, "summary");

                if (summaryElement == null)
                {
                    // DOC200 – Missing <summary>.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.MissingSummary,
                        doc.SpanStart));
                    continue;
                }

                if (!HasMeaningfulContent(summaryElement))
                {
                    // DOC210 – Empty <summary>.
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.EmptySummary,
                        summaryElement.SpanStart));
                }
            }

            return findings;
        }

        /// <summary>
        /// Tries to extract the XML documentation trivia attached to the given member declaration.
        /// </summary>
        /// <param name="member">The member declaration to inspect.</param>
        /// <returns>
        /// The <see cref="DocumentationCommentTriviaSyntax"/> if a documentation comment is present;
        /// otherwise null.
        /// </returns>
        private static DocumentationCommentTriviaSyntax? TryGetDocComment(MemberDeclarationSyntax member)
        {
            SyntaxTriviaList leadingTrivia = member.GetLeadingTrivia();

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
        /// Finds the first XML element with the given local name within the provided documentation comment.
        /// </summary>
        /// <param name="doc">The documentation comment syntax node.</param>
        /// <param name="localName">The expected local element name (e.g. summary in c-tag).</param>
        /// <returns>The first matching <see cref="XmlElementSyntax"/>, or null if none exists.</returns>
        private static XmlElementSyntax? FindFirstElement(DocumentationCommentTriviaSyntax doc, string localName)
        {
            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string name = element.StartTag.Name.LocalName.Text;
                if (string.Equals(name, localName, StringComparison.Ordinal))
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given XML element contains meaningful content.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns>
        /// True if the element contains non-whitespace text or any non-text XML node;
        /// otherwise false.
        /// </returns>
        private static bool HasMeaningfulContent(XmlElementSyntax element)
        {
            // Any non-whitespace text counts.
            // Any non-text node (e.g., <see/>, <para>, nested tags) also counts as content.
            foreach (XmlNodeSyntax node in element.Content)
            {
                if (node is XmlTextSyntax text)
                {
                    if (ContainsNonWhitespace(text))
                    {
                        return true;
                    }

                    continue;
                }

                // Any other XML node counts as content.
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given XML text node contains any non-whitespace characters.
        /// </summary>
        /// <param name="text">The XML text node to inspect.</param>
        /// <returns>True if non-whitespace content exists; otherwise false.</returns>
        private static bool ContainsNonWhitespace(XmlTextSyntax text)
        {
            foreach (SyntaxToken token in text.TextTokens)
            {
                string value = token.ValueText;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an anchor position within the member declaration that is suitable for reporting a finding.
        /// </summary>
        /// <param name="member">The member declaration to anchor the finding to.</param>
        /// <returns>The absolute position in the syntax tree used for line/column calculation.</returns>
        private static int GetAnchorPosition(MemberDeclarationSyntax member)
        {
            // Prefer the identifier token if available, otherwise use first token.
            if (member is BaseTypeDeclarationSyntax typeDecl)
            {
                return typeDecl.Identifier.SpanStart;
            }

            if (member is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.Identifier.SpanStart;
            }

            if (member is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.Identifier.SpanStart;
            }

            if (member is ConstructorDeclarationSyntax ctorDecl)
            {
                return ctorDecl.Identifier.SpanStart;
            }

            if (member is PropertyDeclarationSyntax propDecl)
            {
                return propDecl.Identifier.SpanStart;
            }

            if (member is EventDeclarationSyntax eventDecl)
            {
                return eventDecl.Identifier.SpanStart;
            }

            if (member is EnumMemberDeclarationSyntax enumMemberDecl)
            {
                return enumMemberDecl.Identifier.SpanStart;
            }

            if (member is EventFieldDeclarationSyntax eventFieldDecl)
            {
                VariableDeclaratorSyntax? variable = eventFieldDecl.Declaration.Variables.FirstOrDefault();
                if (variable != null)
                {
                    return variable.Identifier.SpanStart;
                }
            }

            if (member is FieldDeclarationSyntax fieldDecl)
            {
                VariableDeclaratorSyntax? variable = fieldDecl.Declaration.Variables.FirstOrDefault();
                if (variable != null)
                {
                    return variable.Identifier.SpanStart;
                }
            }

            return member.GetFirstToken().SpanStart;
        }
    }
}
