using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects inheritdoc-related documentation smells that can be determined syntactically.
    /// </summary>
    /// <remarks>
    /// This detector currently implements:
    /// <list type="bullet">
    /// <item><description>DOC700: <c>inheritdoc</c> is combined with an explicit <c>summary</c>.</description></item>
    /// </list>
    /// Semantic inheritdoc checks such as source resolution belong to a later semantic detector.
    /// </remarks>
    internal static class XmlDocInheritdocDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns inheritdoc-related findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindInheritdocSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<SyntaxNode> nodes =
                root.DescendantNodes()
                    .Where(node =>
                        node is MemberDeclarationSyntax
                        || node is EnumMemberDeclarationSyntax);

            foreach (SyntaxNode node in nodes)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(node);
                if (doc == null || !doc.HasInheritdoc())
                {
                    continue;
                }

                XmlElementSyntax? summaryElement = XmlDocElementQuery.FirstByName(doc, "summary");
                if (summaryElement != null)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "inheritdoc",
                        XmlDocSmells.InheritdocWithOwnSummary,
                        summaryElement.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(summaryElement)));
                }
            }

            return findings;
        }
    }
}