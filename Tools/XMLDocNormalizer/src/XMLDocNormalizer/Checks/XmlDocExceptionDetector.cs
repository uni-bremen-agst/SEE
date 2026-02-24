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
                    List<ExceptionDocTag> tags = ExtractExceptionTags(doc);

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
        /// Extracts all <exception> tags from a documentation comment and captures their cref strings.
        /// </summary>
        /// <param name="doc">The documentation comment.</param>
        /// <returns>A list of extracted exception tags.</returns>
        private static List<ExceptionDocTag> ExtractExceptionTags(DocumentationCommentTriviaSyntax doc)
        {
            List<ExceptionDocTag> tags = new();

            IEnumerable<XmlElementSyntax> elements =
                XmlDocElementQuery.ElementsByName(doc, "exception");

            foreach (XmlElementSyntax element in elements)
            {
                string? cref = TryGetCrefValue(element);
                tags.Add(new ExceptionDocTag(element, cref));
            }

            return tags;
        }

        /// <summary>
        /// Tries to get the raw cref value string from an <exception> element.
        /// </summary>
        /// <param name="element">The exception element.</param>
        /// <returns>The cref string if present; otherwise <see langword="null"/>.</returns>
        private static string? TryGetCrefValue(XmlElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.StartTag.Attributes)
            {
                if (attribute is XmlCrefAttributeSyntax crefAttribute)
                {
                    return crefAttribute.Cref?.ToString();
                }
            }

            // Missing cref is DOC600 (well-formed detector).
            return null;
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
            List<ExceptionDocTag> tags)
        {
            Dictionary<string, List<ExceptionDocTag>> grouped = new(StringComparer.Ordinal);

            foreach (ExceptionDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawCref))
                {
                    continue;
                }

                if (!grouped.TryGetValue(tag.RawCref, out List<ExceptionDocTag>? list))
                {
                    list = new List<ExceptionDocTag>();
                    grouped.Add(tag.RawCref, list);
                }

                list.Add(tag);
            }

            foreach ((string rawCref, List<ExceptionDocTag> occurrences) in grouped)
            {
                if (occurrences.Count <= 1)
                {
                    continue;
                }

                for (int i = 1; i < occurrences.Count; i++)
                {
                    ExceptionDocTag duplicate = occurrences[i];

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
            List<ExceptionDocTag> tags)
        {
            foreach (ExceptionDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawCref))
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
                        tag.RawCref));
                }
            }
        }

        /// <summary>
        /// Represents an extracted <exception> documentation element with its raw cref string.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="ExceptionDocTag"/> struct.
        /// </remarks>
        /// <param name="element">The XML element.</param>
        /// <param name="rawCref">The raw cref string.</param>
        private readonly struct ExceptionDocTag(XmlElementSyntax element, string? rawCref)
        {
            /// <summary>
            /// Gets the exception XML element.
            /// </summary>
            public XmlElementSyntax Element { get; } = element;

            /// <summary>
            /// Gets the raw cref string as written in the source.
            /// </summary>
            public string? RawCref { get; } = rawCref;
        }
    }
}