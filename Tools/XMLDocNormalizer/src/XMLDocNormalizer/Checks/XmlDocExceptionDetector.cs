using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects exception documentation smells that can be determined without semantic analysis:
    /// <list type="bullet">
    /// <item><description>DOC620: An <exception> tag exists but its description is empty.</description></item>
    /// <item><description>DOC640: A rethrow statement (<c>throw;</c>) was detected.</description></item>
    /// <item><description>DOC650: Multiple <exception> tags exist for the same cref string.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// This detector intentionally does not implement semantic mapping (DOC610/DOC630/DOC660/DOC670).
    /// Those belong to the semantic exception detector.
    /// </remarks>
    internal static class XmlDocExceptionDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for DOC620/DOC640/DOC650.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);
                if (doc != null)
                {
                    List<ExtractedXmlDocTag> tags =
                        XmlDocTagExtraction.ExtractTags(doc, "exception", ExtractExceptionCref);

                    AddDuplicateExceptionTagFindings(findings, tree, filePath, tags);
                    AddEmptyExceptionDescriptionFindings(findings, tree, filePath, tags);
                }

                // DOC640 is independent of documentation and depends on body presence.
                if (SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? bodyNode) && bodyNode != null)
                {
                    if (SyntaxUtils.ContainsRethrow(bodyNode, out int rethrowAnchor))
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "exception",
                            XmlDocSmells.RethrowCannotInferException,
                            rethrowAnchor,
                            snippet: "throw;"));
                    }
                }
            }

            return findings;
        }

        /// <summary>
        /// Extracts the raw cref value from an <exception> XML element.
        /// </summary>
        /// <param name="element">The exception XML element.</param>
        /// <returns>The raw cref value if present; otherwise null.</returns>
        private static string? ExtractExceptionCref(XmlElementSyntax element)
        {
            XmlDocTagExtraction.TryGetCrefAttributeValue(element, out string? cref);
            return cref;
        }

        /// <summary>
        /// Adds DOC650 findings for duplicate exception tags (same raw cref string).
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddDuplicateExceptionTagFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExtractedXmlDocTag> tags)
        {
            Dictionary<string, List<ExtractedXmlDocTag>> grouped = new(StringComparer.Ordinal);

            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }

                if (!grouped.TryGetValue(tag.RawAttributeValue, out List<ExtractedXmlDocTag>? list))
                {
                    list = new();
                    grouped.Add(tag.RawAttributeValue, list);
                }

                list.Add(tag);
            }

            foreach ((string rawCref, List<ExtractedXmlDocTag> occurrences) in grouped)
            {
                if (occurrences.Count <= 1)
                {
                    continue;
                }

                for (int i = 1; i < occurrences.Count; i++)
                {
                    ExtractedXmlDocTag duplicate = occurrences[i];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.DuplicateExceptionTag,
                        duplicate.Element.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(duplicate.Element),
                        rawCref));
                }
            }
        }

        /// <summary>
        /// Adds DOC620 findings for exception tags with empty descriptions.
        /// </summary>
        /// <param name="findings">The finding sink.</param>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="tags">The extracted exception tags.</param>
        private static void AddEmptyExceptionDescriptionFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            List<ExtractedXmlDocTag> tags)
        {
            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    // DOC600 handles missing cref.
                    continue;
                }

                if (!XmlDocUtils.HasMeaningfulContent(tag.Element))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "exception",
                        XmlDocSmells.EmptyExceptionDescription,
                        tag.Element.SpanStart,
                        snippet: SyntaxUtils.GetSnippet(tag.Element),
                        tag.RawAttributeValue));
                }
            }
        }
    }
}