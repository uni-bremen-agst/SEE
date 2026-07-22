using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects inheritdoc-related documentation smells that can be determined syntactically.
    /// </summary>
    /// <remarks>
    /// This detector currently implements:
    /// <list type="bullet">
    /// <item><description>DOC700: <c>inheritdoc</c> is combined with an explicit <c>summary</c>.</description></item>
    /// <item><description>DOC750: Multiple <c>inheritdoc</c> tags are present on the same declaration.</description></item>
    /// </list>
    /// Semantic inheritdoc checks such as source resolution belong to the semantic detector.
    /// </remarks>
    internal static class XmlDocInheritdocDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns inheritdoc-related findings.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters and an inheritdoc
        /// finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of an inheritdoc or summary tag does
        /// not identify a valid position in <paramref name="tree"/> and a finding
        /// is created.
        /// </exception>
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
                if (doc == null)
                {
                    continue;
                }

                List<XmlNodeSyntax> inheritdocNodes =
                [
                    .. XmlDocTagExtraction.EmptyElementsByName(doc, "inheritdoc"),
                    .. XmlDocElementQuery.ElementsByName(doc, "inheritdoc"),
                ];

                inheritdocNodes = inheritdocNodes
                    .OrderBy(inheritdocNode => inheritdocNode.SpanStart)
                    .ToList();

                if (inheritdocNodes.Count == 0)
                {
                    continue;
                }

                if (inheritdocNodes.Count >= 2)
                {
                    XmlNodeSyntax secondInheritdoc = inheritdocNodes[1];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "inheritdoc",
                        XmlDocSmells.DuplicateInheritdocTag,
                        secondInheritdoc.SpanStart,
                        CreateInheritdocContext(
                            node,
                            targetName: "inheritdoc",
                            filePath: filePath),
                        snippet: SyntaxUtils.GetSnippet(secondInheritdoc)));
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
                        CreateInheritdocContext(
                            node,
                            targetName: "summary",
                            filePath: filePath),
                        snippet: SyntaxUtils.GetSnippet(summaryElement)));
                }
            }

            return findings;
        }

        /// <summary>
        /// Creates finding context metadata for an inheritdoc-related finding.
        /// </summary>
        /// <param name="node">The declaration node that owns the inheritdoc documentation.</param>
        /// <param name="targetName">The affected inheritdoc target or related XML tag name.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A populated finding context for an inheritdoc finding.
        /// </returns>
        private static FindingContext CreateInheritdocContext(
            SyntaxNode node,
            string? targetName,
            string filePath)
        {
            return FindingContextBuilder.ForDeclaration(
                node,
                "InheritdocTag",
                targetName: targetName,
                filePath: filePath);
        }
    }
}