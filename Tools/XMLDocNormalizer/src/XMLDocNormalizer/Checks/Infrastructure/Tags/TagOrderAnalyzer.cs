using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Tags
{
    /// <summary>
    /// Analyzes the order of XML documentation tags that reference declared names.
    /// </summary>
    /// <remarks>
    /// This analyzer is used for ordered documentation tags such as param and typeparam.
    /// It reports at most one order mismatch finding per documented declaration and deliberately avoids
    /// follow-up findings when missing, unknown, or duplicate tag references are present.
    /// </remarks>
    internal static class TagOrderAnalyzer
    {
        /// <summary>
        /// Adds a single order-mismatch finding if documentation tags do not follow the declared name order.
        /// </summary>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="tree">The syntax tree containing the declaration.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="declaration">The declaration that owns the documentation comment.</param>
        /// <param name="xmlTagName">The XML tag name being analyzed.</param>
        /// <param name="declaredNames">The set of declared names.</param>
        /// <param name="declaredOrder">The declared name order.</param>
        /// <param name="tags">The extracted documentation tags.</param>
        /// <param name="orderMismatchSmell">The smell emitted when the documentation tag order is wrong.</param>
        /// <param name="subjectKind">The finding context subject kind to use for the produced finding.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="orderMismatchSmell"/> is
        /// <see langword="null"/> and an order mismatch finding is created.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> or
        /// <paramref name="xmlTagName"/> is <see langword="null"/>, empty,
        /// or consists only of white-space characters and an order mismatch
        /// finding is created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the source position of the first documented tag does not
        /// identify a valid position in <paramref name="tree"/> and an order
        /// mismatch finding is created.
        /// </exception>
        public static void AddOrderMismatchFinding(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            SyntaxNode declaration,
            string xmlTagName,
            IReadOnlySet<string> declaredNames,
            IReadOnlyList<string> declaredOrder,
            IReadOnlyList<ExtractedXmlDocTag> tags,
            XmlDocSmell orderMismatchSmell,
            string subjectKind)
        {
            if (declaredOrder.Count < 2)
            {
                return;
            }

            if (tags.Count < 2)
            {
                return;
            }

            List<string> documentedOrder = new List<string>();
            HashSet<string> seenDocumentedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ExtractedXmlDocTag tag in tags)
            {
                string? documentedName = tag.RawAttributeValue;

                if (string.IsNullOrWhiteSpace(documentedName))
                {
                    return;
                }

                if (!declaredNames.Contains(documentedName))
                {
                    return;
                }

                if (!seenDocumentedNames.Add(documentedName))
                {
                    return;
                }

                documentedOrder.Add(documentedName);
            }

            if (documentedOrder.Count != declaredOrder.Count)
            {
                return;
            }

            if (documentedOrder.SequenceEqual(declaredOrder, StringComparer.Ordinal))
            {
                return;
            }

            ExtractedXmlDocTag firstDocumentedTag = tags[0];

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: xmlTagName,
                orderMismatchSmell,
                firstDocumentedTag.Element.SpanStart,
                FindingContextBuilder.ForDeclaration(
                    declaration,
                    subjectKind,
                    targetName: null,
                    filePath: filePath),
                snippet: SyntaxUtils.GetSnippet(firstDocumentedTag.Element)));
        }
    }
}
