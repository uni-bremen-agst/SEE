using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects malformed XML documentation tags in a syntax tree.
    /// </summary>
    internal static class XmlDocWellFormedChecker
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for malformed XML doc tags.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindMalformedTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<DocumentationCommentTriviaSyntax> docTrivias =
                root.DescendantTrivia(descendIntoTrivia: true)
                    .Select(t => t.GetStructure())
                    .OfType<DocumentationCommentTriviaSyntax>();

            foreach (DocumentationCommentTriviaSyntax doc in docTrivias)
            {
                IEnumerable<XmlElementSyntax> elements =
                    doc.DescendantNodes()
                        .OfType<XmlElementSyntax>();

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    // Missing end tag.
                    if (element.EndTag == null)
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "Missing end tag (unclosed XML element).");
                        continue;
                    }

                    // Common mistake: <paramref> and <typeparamref> should be empty elements (<.../>).
                    if (tagName == "paramref" || tagName == "typeparamref")
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "This tag should be an empty element, e.g. <paramref name=\"x\"/>.");
                        continue;
                    }
                }
            }

            return findings;
        }

        private static void AddFinding(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            SyntaxNode node,
            string tagName,
            string message)
        {
            FileLinePositionSpan span = tree.GetLineSpan(node.Span);
            int line = span.StartLinePosition.Line + 1;
            int column = span.StartLinePosition.Character + 1;

            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            findings.Add(new Finding(filePath, tagName, line, column, message, snippet));
        }
    }
}
