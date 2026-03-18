using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Reporting.Statistics
{
    /// <summary>
    /// Collects top-level XML documentation tag order observations from syntax trees.
    /// </summary>
    internal static class XmlDocTopLevelTagOrderStatisticsCollector
    {
        /// <summary>
        /// Collects aggregated top-level tag order statistics from one syntax tree.
        /// </summary>
        /// <param name="root">The compilation unit root to inspect.</param>
        /// <param name="projectName">The logical project name.</param>
        /// <returns>The aggregated project statistics.</returns>
        public static TopLevelTagOrderProjectStatistics Collect(
            CompilationUnitSyntax root,
            string projectName)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(projectName);

            TopLevelTagOrderProjectStatistics statistics = new TopLevelTagOrderProjectStatistics
            {
                ProjectName = projectName
            };

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                if (doc == null)
                {
                    continue;
                }

                List<string> rawSequence = GetRelevantTopLevelTagSequence(doc);

                if (rawSequence.Count == 0)
                {
                    continue;
                }

                List<string> collapsedSequence = CollapseRepeatableNeighborTags(rawSequence);

                TopLevelTagOrderObservation observation =
                    new TopLevelTagOrderObservation(
                        GetMemberKind(member),
                        rawSequence,
                        collapsedSequence);

                statistics.AddObservation(observation);
            }

            return statistics;
        }

        /// <summary>
        /// Gets the relevant top-level tag sequence of a documentation comment.
        /// </summary>
        /// <param name="doc">The documentation comment to inspect.</param>
        /// <returns>The ordered top-level tag sequence.</returns>
        private static List<string> GetRelevantTopLevelTagSequence(DocumentationCommentTriviaSyntax doc)
        {
            List<string> tags = new List<string>();

            foreach (XmlNodeSyntax node in doc.Content)
            {
                string? tagName = GetTopLevelTagName(node);

                if (tagName == null)
                {
                    continue;
                }

                if (!IsRelevantTag(tagName))
                {
                    continue;
                }

                tags.Add(tagName);
            }

            return tags;
        }

        /// <summary>
        /// Collapses consecutive repeatable tags into a single representative tag.
        /// </summary>
        /// <param name="rawSequence">The raw top-level tag sequence.</param>
        /// <returns>The normalized sequence.</returns>
        private static List<string> CollapseRepeatableNeighborTags(IReadOnlyList<string> rawSequence)
        {
            List<string> collapsed = new List<string>();
            string? previousTag = null;

            for (int i = 0; i < rawSequence.Count; i++)
            {
                string currentTag = rawSequence[i];

                if ((previousTag == currentTag) && IsRepeatableBlockTag(currentTag))
                {
                    continue;
                }

                collapsed.Add(currentTag);
                previousTag = currentTag;
            }

            return collapsed;
        }

        /// <summary>
        /// Determines whether a tag should be treated as a repeatable block tag
        /// for order-analysis normalization.
        /// </summary>
        /// <param name="tagName">The tag name to inspect.</param>
        /// <returns><see langword="true"/> if the tag should be collapsed in repeated runs; otherwise <see langword="false"/>.</returns>
        private static bool IsRepeatableBlockTag(string tagName)
        {
            return tagName == "typeparam"
                || tagName == "param"
                || tagName == "exception";
        }

        /// <summary>
        /// Gets the local tag name of a top-level XML documentation node.
        /// </summary>
        /// <param name="node">The XML node to inspect.</param>
        /// <returns>The tag name if available; otherwise <see langword="null"/>.</returns>
        private static string? GetTopLevelTagName(XmlNodeSyntax node)
        {
            if (node is XmlElementSyntax element)
            {
                return element.StartTag.Name.LocalName.Text;
            }

            if (node is XmlEmptyElementSyntax emptyElement)
            {
                return emptyElement.Name.LocalName.Text;
            }

            return null;
        }

        /// <summary>
        /// Determines whether a tag is relevant for top-level order analysis.
        /// </summary>
        /// <param name="tagName">The tag name to inspect.</param>
        /// <returns><see langword="true"/> if the tag is relevant; otherwise <see langword="false"/>.</returns>
        private static bool IsRelevantTag(string tagName)
        {
            return tagName == "summary"
                || tagName == "typeparam"
                || tagName == "param"
                || tagName == "returns"
                || tagName == "value"
                || tagName == "exception"
                || tagName == "remarks"
                || tagName == "seealso";
        }

        /// <summary>
        /// Gets a readable member kind for statistics output.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns>The member kind name.</returns>
        private static string GetMemberKind(MemberDeclarationSyntax member)
        {
            return member.Kind().ToString();
        }
    }
}
