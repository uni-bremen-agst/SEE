using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects XML documentation tags that are not allowed on the documented member kind.
    /// </summary>
    /// <remarks>
    /// This detector reports invalid generic tag placement.
    /// Specialized tags that are handled by dedicated detectors are skipped here to avoid duplicate findings.
    /// </remarks>
    internal static class XmlDocMemberTagDetector
    {
        /// <summary>
        /// Analyzes a syntax tree and returns findings for XML documentation tags that are not allowed on their owner member.
        /// </summary>
        /// <param name="tree">The syntax tree containing members to check.</param>
        /// <param name="filePath">The source file path used in reporting findings.</param>
        /// <returns>
        /// A list of findings for invalid XML documentation tags on members.
        /// </returns>
        public static List<Finding> FindInvalidTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            IEnumerable<SyntaxNode> nodes = tree
                .GetRoot()
                .DescendantNodes()
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

                IEnumerable<XmlElementSyntax> elements = XmlDocElementQuery.Elements(doc);

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    if (AllowedTagMatrix.IsHandledBySpecializedDetector(tagName))
                    {
                        continue;
                    }

                    if (AllowedTagMatrix.IsTagAllowed(node, tagName))
                    {
                        continue;
                    }

                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.InvalidTagOnMember,
                        element.Span,
                        FindingContextBuilder.ForDeclaration(
                            node,
                            "InvalidTagUsage",
                            targetName: tagName,
                            filePath: filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));
                }
            }

            return findings;
        }
    }
}
