using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Tags
{
    /// <summary>
    /// Provides a shared analysis routine for name-based XML documentation tags such as param and typeparam.
    /// </summary>
    internal static class NamedTagAnalyzer
    {
        /// <summary>
        /// Analyzes the relationship between declared names and documented XML tags that reference names by attribute.
        /// </summary>
        /// <param name="findings">The finding sink to add findings to.</param>
        /// <param name="tree">The syntax tree used for line and column calculation.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="declaredNames">The set of declared names.</param>
        /// <param name="docTags">The list of documented tags with extracted names.</param>
        /// <param name="smells">The smell mapping for missing, empty, unknown, and duplicate findings.</param>
        /// <param name="missingAnchorProvider">
        /// A function that returns the absolute anchor position in the source for a missing documentation finding.
        /// </param>
        /// <param name="hasMeaningfulContent">
        /// A function that determines whether a documentation element contains meaningful content.
        /// </param>
        /// <param name="snippetProvider">A function that creates a short snippet for a syntax node.</param>
        /// <param name="contextProvider">
        /// An optional function that creates finding context metadata for the affected declared or documented name.
        /// </param>
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
            Func<SyntaxNode, string> snippetProvider,
            Func<string, FindingContext>? contextProvider = null)
        {
            Dictionary<string, List<ExtractedXmlDocTag>> tagsByName = GroupByName(docTags);

            foreach ((string name, List<ExtractedXmlDocTag> tags) in tagsByName)
            {
                if (tags.Count <= 1)
                {
                    continue;
                }

                for (int i = 1; i < tags.Count; i++)
                {
                    ExtractedXmlDocTag tag = tags[i];

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: xmlTagName,
                        smells.DuplicateTag,
                        tag.Element.SpanStart,
                        CreateContext(contextProvider, name),
                        snippet: snippetProvider(tag.Element),
                        name));
                }
            }

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
                        CreateContext(contextProvider, tag.RawAttributeValue),
                        snippet: snippetProvider(tag.Element),
                        tag.RawAttributeValue));
                }
            }

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
                    CreateContext(contextProvider, declaredName),
                    snippet: string.Empty,
                    declaredName));
            }

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
                    CreateContext(contextProvider, documentedName),
                    snippet: snippetProvider(first.Element),
                    documentedName));
            }
        }

        /// <summary>
        /// Extracts the referenced name from a name-based XML documentation element.
        /// </summary>
        /// <param name="element">The XML documentation element to inspect.</param>
        /// <returns>
        /// The extracted name value if present; otherwise null.
        /// </returns>
        public static string? ExtractReferencedName(XmlElementSyntax element)
        {
            return XmlDocTagExtraction.TryGetNameAttributeValue(element);
        }

        /// <summary>
        /// Groups named documentation tags by their extracted name.
        /// </summary>
        /// <param name="tags">The tags to group.</param>
        /// <returns>
        /// A dictionary mapping each extracted name to its tag occurrences.
        /// </returns>
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
                    list = new List<ExtractedXmlDocTag>();
                    grouped.Add(name, list);
                }

                list.Add(tag);
            }

            return grouped;
        }

        /// <summary>
        /// Creates finding context metadata for an affected name if a context provider is available.
        /// </summary>
        /// <param name="contextProvider">The optional context provider.</param>
        /// <param name="name">The affected declared or documented name.</param>
        /// <returns>
        /// A finding context if a provider is available; otherwise null.
        /// </returns>
        private static FindingContext? CreateContext(
            Func<string, FindingContext>? contextProvider,
            string name)
        {
            if (contextProvider == null)
            {
                return null;
            }

            return contextProvider(name);
        }
    }
}
