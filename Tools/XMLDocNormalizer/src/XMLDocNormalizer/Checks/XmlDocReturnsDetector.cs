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

                List<XmlElementSyntax> returnsTags = XmlDocElementQuery.ElementsByName(doc, "returns").ToList();

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
                        MemberAnchorResolver.GetAnchorPosition(member),
                        snippet: string.Empty));

                    continue;
                }

                XmlElementSyntax returnsElement = returnsTags[0];

                if (!XmlDocUtils.HasMeaningfulContent(returnsElement))
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
    }
}
