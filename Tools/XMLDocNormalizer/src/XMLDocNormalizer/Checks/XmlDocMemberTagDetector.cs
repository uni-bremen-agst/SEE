using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects DOC140 – XML documentation tags that are not allowed on the member type.
    /// Reports tags like <returns> on void methods, <value> on fields, <param> on parameterless constructors, etc.
    /// </summary>
    internal static class XmlDocMemberTagDetector
    {
        /// <summary>
        /// Analyzes a syntax tree and returns all findings for tags that are not allowed on their member.
        /// </summary>
        /// <param name="tree">The syntax tree containing members to check.</param>
        /// <param name="filePath">The source file path used in reporting findings.</param>
        /// <returns>A list of findings for invalid XML documentation tags.</returns>
        public static List<Finding> FindInvalidTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            IEnumerable<SyntaxNode> nodes = tree
                .GetRoot()
                .DescendantNodes()
                .Where(n =>
                    n is MemberDeclarationSyntax
                    || n is EnumMemberDeclarationSyntax);


            foreach (SyntaxNode node in nodes)
            {
                DocumentationCommentTriviaSyntax? docComment = XmlDocUtils.TryGetDocComment(node);

                if (docComment == null)
                {
                    continue;
                }

                IEnumerable<XmlElementSyntax> elements = docComment.DescendantNodes().OfType<XmlElementSyntax>();

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    if (!AllowedTagMatrix.IsTagAllowed(node, tagName))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.InvalidTagOnMember,
                            element.Span,
                            snippet: SyntaxUtils.GetSnippet(element)));
                    }
                }
            }

            return findings;
        }
    }
}
