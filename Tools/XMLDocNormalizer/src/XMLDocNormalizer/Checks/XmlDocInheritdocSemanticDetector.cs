using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects inheritdoc-related smells that require semantic analysis.
    /// </summary>
    internal static class XmlDocInheritdocSemanticDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns inheritdoc-related findings that require semantic analysis.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindInheritdocSmells(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel)
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

                foreach (XmlEmptyElementSyntax inheritdocElement in XmlDocTagExtraction.EmptyElementsByName(doc, "inheritdoc"))
                {
                    XmlCrefAttributeSyntax? crefAttribute =
                        SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(inheritdocElement, "cref");

                    if (crefAttribute == null)
                    {
                        continue;
                    }

                    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref);

                    if (symbolInfo.Symbol != null)
                    {
                        continue;
                    }

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "inheritdoc",
                        XmlDocSmells.InvalidInheritdocCref,
                        crefAttribute.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(inheritdocElement)));
                }
            }

            return findings;
        }
    }
}