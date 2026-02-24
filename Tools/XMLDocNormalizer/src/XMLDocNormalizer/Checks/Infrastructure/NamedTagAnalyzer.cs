using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides a shared analysis routine for name-based XML documentation tags such as
    /// param name="..." and typeparam name="...".
    /// </summary>
    internal static class NamedTagAnalyzer
    {
        /// <summary>
        /// Analyzes the relationship between declared names (e.g. parameters/type parameters) and
        /// documented XML tags that reference names via a name attribute.
        /// </summary>
        /// <param name="findings">The finding sink to add findings to.</param>
        /// <param name="tree">The syntax tree used for line/column calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="xmlTagName">The XML tag name ("param" or "typeparam").</param>
        /// <param name="declaredNames">The set of declared names.</param>
        /// <param name="docTags">The list of documented tags with extracted names.</param>
        /// <param name="smells">The smell mapping for missing/empty/unknown/duplicate.</param>
        /// <param name="missingAnchorProvider">
        /// A function that returns the absolute anchor position in the source for a missing documentation finding
        /// for the provided declared name.
        /// </param>
        /// <param name="hasMeaningfulContent">
        /// A function that determines whether a documentation element contains meaningful content.
        /// </param>
        /// <param name="snippetProvider">A function that creates a short snippet for a syntax node.</param>
        public static void Analyze(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            string xmlTagName,
            IReadOnlyCollection<string> declaredNames,
            IReadOnlyList<ExtractedXmlDocTag> docTags,
            NamedTagSmellSet smells,
            Func<string, int> missingAnchorProvider,
            Func<XmlElementSyntax, bool> hasMeaningfulContent,
            Func<SyntaxNode, string> snippetProvider)
        {
            Dictionary<string, List<ExtractedXmlDocTag>> tagsByName = GroupByName(docTags);

            // Duplicate (e.g. multiple <param name="x">)
            foreach ((string name, List<ExtractedXmlDocTag> tags) in tagsByName)
            {
                if (tags.Count <= 1)
                {
                    continue;
                }

                // Report duplicates starting at the second occurrence.
                for (int i = 1; i < tags.Count; i++)
                {
                    ExtractedXmlDocTag tag = tags[i];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: xmlTagName,
                        smells.DuplicateTag,
                        tag.Element.SpanStart,
                        snippet: snippetProvider(tag.Element),
                        name));
                }
            }

            // Empty description (only for tags that exist)
            foreach (ExtractedXmlDocTag tag in docTags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }
                if (!hasMeaningfulContent(tag.Element))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: xmlTagName,
                        smells.EmptyDescription,
                        tag.Element.SpanStart,
                        snippet: snippetProvider(tag.Element),
                        tag.RawAttributeValue));
                }
            }

            // Missing tag (declared but not documented)
            foreach (string declaredName in declaredNames)
            {
                if (tagsByName.ContainsKey(declaredName))
                {
                    continue;
                }

                int anchor = missingAnchorProvider(declaredName);

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: xmlTagName,
                    smells.MissingTag,
                    anchor,
                    snippet: string.Empty,
                    declaredName));
            }

            // Unknown tag (documented but not declared)
            foreach ((string documentedName, List<ExtractedXmlDocTag> tags) in tagsByName)
            {
                if (declaredNames.Contains(documentedName))
                {
                    continue;
                }

                ExtractedXmlDocTag first = tags[0];

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: xmlTagName,
                    smells.UnknownTag,
                    first.Element.SpanStart,
                    snippet: snippetProvider(first.Element),
                    documentedName));
            }
        }

        /// <summary>
        /// Groups named documentation tags by their extracted name.
        /// </summary>
        /// <param name="tags">The tags to group.</param>
        /// <returns>A dictionary mapping name to tag occurrences.</returns>
        private static Dictionary<string, List<ExtractedXmlDocTag>> GroupByName(IReadOnlyList<ExtractedXmlDocTag> tags)
        {
            Dictionary<string, List<ExtractedXmlDocTag>> grouped = new(StringComparer.Ordinal);

            foreach (ExtractedXmlDocTag tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.RawAttributeValue))
                {
                    continue;
                }
                string name = tag.RawAttributeValue;
                if (!grouped.TryGetValue(name, out List<ExtractedXmlDocTag>? list))
                {
                    list = new();
                    grouped.Add(name, list);
                }

                list.Add(tag);
            }

            return grouped;
        }

        /// <summary>
        /// Extracts the referenced name from a name-based XML documentation element
        /// (e.g. &lt;param name="..."&gt;, &lt;typeparam name="..."&gt;).
        /// </summary>
        /// <param name="element">
        /// The XML documentation element.
        /// </param>
        /// <returns>
        /// The extracted name value if present; otherwise <see langword="null"/>.
        /// </returns>
        public static string? ExtractReferencedName(XmlElementSyntax element)
        {
            return XmlDocTagExtraction.TryGetNameAttributeValue(element);
        }
    }
}
