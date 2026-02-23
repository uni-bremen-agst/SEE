using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects returns documentation smells:
    /// - DOC500: A non-void member has no returns documentation.
    /// - DOC510: The returns tag exists but its description is empty.
    /// - DOC520: A void member contains a returns tag.
    /// - DOC530: Multiple returns tags exist.
    /// </summary>
    internal static class XmlDocReturnsDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC500/DOC510/DOC520/DOC530.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindReturnsSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!SupportsReturns(member))
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);
                if (doc == null)
                {
                    // Missing overall documentation is handled by DOC100/basic detector.
                    continue;
                }

                List<XmlElementSyntax> returnsTags = GetReturnsElements(doc);

                bool isVoid = IsVoidLike(member);

                // DOC530: duplicate <returns>
                if (returnsTags.Count > 1)
                {
                    for (int i = 1; i < returnsTags.Count; i++)
                    {
                        XmlElementSyntax element = returnsTags[i];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "returns",
                            XmlDocSmells.DuplicateReturnsTag,
                            element.SpanStart,
                            snippet: SyntaxUtils.GetSnippet(element)));
                    }
                }

                // DOC520: returns on void member
                if (isVoid)
                {
                    if (returnsTags.Count > 0)
                    {
                        XmlElementSyntax first = returnsTags[0];

                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "returns",
                            XmlDocSmells.ReturnsOnVoidMember,
                            first.SpanStart,
                            snippet: SyntaxUtils.GetSnippet(first)));
                    }

                    continue;
                }

                // Non-void member: missing/empty returns
                if (returnsTags.Count == 0)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "returns",
                        XmlDocSmells.MissingReturns,
                        GetAnchorPosition(member),
                        snippet: string.Empty));

                    continue;
                }

                XmlElementSyntax returnsElement = returnsTags[0];

                if (!HasMeaningfulContent(returnsElement))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "returns",
                        XmlDocSmells.EmptyReturns,
                        returnsElement.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(returnsElement)));
                }
            }

            return findings;
        }

        /// <summary>
        /// Determines whether the given member kind is eligible for returns checks.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> if returns rules apply; otherwise <see langword="false"/>.</returns>
        private static bool SupportsReturns(MemberDeclarationSyntax member)
        {
            return member is MethodDeclarationSyntax ||
                   member is DelegateDeclarationSyntax ||
                   member is OperatorDeclarationSyntax ||
                   member is ConversionOperatorDeclarationSyntax;
        }

        /// <summary>
        /// Determines whether the given member is considered void-like for returns purposes.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the member is void-like (e.g. method returns void); otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsVoidLike(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.ReturnType is PredefinedTypeSyntax predefined &&
                       predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
            }

            if (member is OperatorDeclarationSyntax operatorDecl)
            {
                return operatorDecl.ReturnType is PredefinedTypeSyntax predefined &&
                       predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
            }

            if (member is ConversionOperatorDeclarationSyntax)
            {
                // Conversions always return a value.
                return false;
            }

            if (member is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.ReturnType is PredefinedTypeSyntax predefined &&
                       predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
            }

            return false;
        }

        /// <summary>
        /// Collects all returns elements from a documentation comment.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <returns>A list of returns elements.</returns>
        private static List<XmlElementSyntax> GetReturnsElements(DocumentationCommentTriviaSyntax doc)
        {
            List<XmlElementSyntax> returnsElements = new();

            IEnumerable<XmlElementSyntax> elements =
                doc.DescendantNodes()
                    .OfType<XmlElementSyntax>();

            foreach (XmlElementSyntax element in elements)
            {
                string localName = element.StartTag.Name.LocalName.Text;
                if (string.Equals(localName, "returns", StringComparison.Ordinal))
                {
                    returnsElements.Add(element);
                }
            }

            return returnsElements;
        }

        /// <summary>
        /// Determines whether the given returns element contains meaningful content.
        /// </summary>
        /// <param name="element">The returns element to inspect.</param>
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

                return true;
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
            if (member is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.Identifier.SpanStart;
            }

            if (member is PropertyDeclarationSyntax propDecl)
            {
                return propDecl.Identifier.SpanStart;
            }

            if (member is IndexerDeclarationSyntax indexerDecl)
            {
                return indexerDecl.ThisKeyword.SpanStart;
            }

            if (member is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.Identifier.SpanStart;
            }

            if (member is OperatorDeclarationSyntax operatorDecl)
            {
                return operatorDecl.OperatorToken.SpanStart;
            }

            if (member is ConversionOperatorDeclarationSyntax conversionDecl)
            {
                return conversionDecl.Type.SpanStart;
            }

            return member.GetFirstToken().SpanStart;
        }
    }
}
