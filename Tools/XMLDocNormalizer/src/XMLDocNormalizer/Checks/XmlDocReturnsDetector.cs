using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation smells related to returns tags.
    /// </summary>
    /// <remarks>
    /// This detector reports missing returns tags, empty returns descriptions, returns tags on void-like members,
    /// and duplicate returns tags.
    /// The analysis is syntax-based and does not require semantic model access.
    /// </remarks>
    internal static class XmlDocReturnsDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for returns documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings produced by the returns documentation detector.
        /// </returns>
        public static List<Finding> FindReturnsSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

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
                    continue;
                }

                FindingContext context = FindingContextBuilder.ForDeclaration(
                    member,
                    "ReturnValue",
                    filePath: filePath);

                List<XmlElementSyntax> returnsTags = XmlDocElementQuery.ElementsByName(doc, "returns").ToList();

                bool isVoid = IsVoidLike(member);

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
                            context,
                            snippet: SyntaxUtils.GetSnippet(element)));
                    }
                }

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
                            context,
                            snippet: SyntaxUtils.GetSnippet(first)));
                    }

                    continue;
                }

                if (returnsTags.Count == 0)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "returns",
                        XmlDocSmells.MissingReturns,
                        MemberAnchorResolver.GetAnchorPosition(member),
                        context,
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
                        context,
                        snippet: SyntaxUtils.GetSnippet(returnsElement)));
                }
            }

            return findings;
        }

        /// <summary>
        /// Determines whether returns rules apply to the given member.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// True if returns rules apply; otherwise false.
        /// </returns>
        private static bool SupportsReturns(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax)
            {
                return true;
            }

            if (member is DelegateDeclarationSyntax)
            {
                return true;
            }

            if (member is OperatorDeclarationSyntax)
            {
                return true;
            }

            if (member is ConversionOperatorDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given member is considered void-like for returns checks.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>
        /// True if the member is void-like; otherwise false.
        /// </returns>
        private static bool IsVoidLike(MemberDeclarationSyntax member)
        {
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            if (member is OperatorDeclarationSyntax operatorDeclaration)
            {
                if (operatorDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            if (member is ConversionOperatorDeclarationSyntax)
            {
                return false;
            }

            if (member is DelegateDeclarationSyntax delegateDeclaration)
            {
                if (delegateDeclaration.ReturnType is PredefinedTypeSyntax predefinedReturnType)
                {
                    return predefinedReturnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                }

                return false;
            }

            return false;
        }
    }
}
