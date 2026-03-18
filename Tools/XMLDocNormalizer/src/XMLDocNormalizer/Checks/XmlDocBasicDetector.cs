using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Namespace;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects basic documentation smells:
    /// - DOC100: Missing documentation comment.
    /// - DOC200: Missing summary-tag.
    /// - DOC210: Empty summary-tag.
    /// - DOC150: Top-level tag order mismatch.
    /// - Duplicate summary/remarks tags.
    /// - Empty remarks-tag.
    /// </summary>
    internal static class XmlDocBasicDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for the basic XML documentation smells
        /// with initial <see cref="XmlDocOptions"/>.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath)
        {
            return FindBasicSmells(tree, filePath, new XmlDocOptions());
        }

        /// <summary>
        /// Scans the syntax tree and returns findings for the basic XML documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="options">The XMLDocOptions for this tool run.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(SyntaxTree tree, string filePath, XmlDocOptions options)
        {
            NamespaceDocumentationAggregator aggregartor =
                new(enabled: false);

            return FindBasicSmells(tree, filePath, options, aggregartor);
        }

        /// <summary>
        /// Scans the syntax tree and returns findings for the basic XML documentation smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="options">The XMLDocOptions for this tool run.</param>
        /// <param name="namespaceAggregator">
        /// Aggregates missing namespace documentation into a single DOC101 per (directory, namespace) and enforces
        /// that namespace documentation lives in the preferred dedicated namespace doc file.
        /// </param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicSmells(
            SyntaxTree tree,
            string filePath,
            XmlDocOptions options,
            NamespaceDocumentationAggregator namespaceAggregator)
        {
            ArgumentNullException.ThrowIfNull(namespaceAggregator);

            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<MemberDeclarationSyntax> members =
                root.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                if (!options.CheckEnumMembers && member is EnumMemberDeclarationSyntax)
                {
                    continue;
                }

                if (!options.RequireDocumentationForNamespaces && member is BaseNamespaceDeclarationSyntax)
                {
                    continue;
                }

                DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

                // Namespace handling:
                // - Missing namespace documentation is aggregated (DOC101) and not emitted as DOC100.
                // - Namespace summary validation is only applied in the preferred namespace doc file.
                if (options.RequireDocumentationForNamespaces && member is BaseNamespaceDeclarationSyntax ns)
                {
                    string namespaceName = ns.Name.ToString();

                    if (doc == null)
                    {
                        namespaceAggregator.RegisterMissingNamespaceDocumentation(
                            tree,
                            filePath,
                            namespaceName,
                            MemberAnchorResolver.GetAnchorPosition(member));

                        // Important: no missing documentation for namespaces here; it is emitted at end of run.
                        continue;
                    }

                    namespaceAggregator.RegisterDocumentedNamespace(filePath, namespaceName);

                    if (!namespaceAggregator.IsPreferredNamespaceDocFile(filePath, namespaceName))
                    {
                        continue;
                    }
                }

                if (doc == null)
                {
                    if (member.GetMissingDocumentationSmell() is XmlDocSmell docSmell)
                    {
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "documentation",
                            docSmell,
                            MemberAnchorResolver.GetAnchorPosition(member)));
                    }

                    continue;
                }

                // If fields are allowed to omit <summary>, skip summary-related findings for fields.
                if (!options.RequireSummaryForFields
                    && (member is FieldDeclarationSyntax || member is EventFieldDeclarationSyntax))
                {
                    CheckRemarksSmells(tree, filePath, doc, findings);
                    CheckTopLevelTagOrderSmells(tree, filePath, doc, findings);
                    continue;
                }

                CheckSummarySmells(tree, filePath, doc, findings);
                CheckRemarksSmells(tree, filePath, doc, findings);
                CheckTopLevelTagOrderSmells(tree, filePath, doc, findings);
            }

            return findings;
        }

        /// <summary>
        /// Checks summary-related smells for a documentation comment.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void CheckSummarySmells(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax doc,
            List<Finding> findings)
        {
            List<XmlElementSyntax> summaryElements = XmlDocElementQuery.AllByName(doc, "summary").ToList();

            if (summaryElements.Count == 0)
            {
                if (!doc.HasInheritdoc())
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.MissingSummary,
                        doc.SpanStart));
                }

                return;
            }

            if (summaryElements.Count > 1)
            {
                foreach (XmlElementSyntax summaryElement in summaryElements)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "summary",
                        XmlDocSmells.DuplicateSummaryTag,
                        summaryElement.SpanStart));
                }
            }

            XmlElementSyntax firstSummary = summaryElements[0];

            if (!XmlDocUtils.HasMeaningfulContent(firstSummary))
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "summary",
                    XmlDocSmells.EmptySummary,
                    firstSummary.SpanStart));
            }
        }

        /// <summary>
        /// Checks remarks-related smells for a documentation comment.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void CheckRemarksSmells(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax doc,
            List<Finding> findings)
        {
            List<XmlElementSyntax> remarksElements = XmlDocElementQuery.AllByName(doc, "remarks").ToList();

            if (remarksElements.Count == 0)
            {
                return;
            }

            if (remarksElements.Count > 1)
            {
                foreach (XmlElementSyntax remarksElement in remarksElements)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "remarks",
                        XmlDocSmells.DuplicateRemarksTag,
                        remarksElement.SpanStart));
                }
            }

            foreach (XmlElementSyntax remarksElement in remarksElements)
            {
                if (!XmlDocUtils.HasMeaningfulContent(remarksElement))
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        tagName: "remarks",
                        XmlDocSmells.EmptyRemarks,
                        remarksElement.SpanStart));
                }
            }
        }

        /// <summary>
        /// Checks top-level XML documentation tags for order violations.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void CheckTopLevelTagOrderSmells(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax doc,
            List<Finding> findings)
        {
            List<(string TagName, XmlNodeSyntax Node)> orderedTags = new List<(string TagName, XmlNodeSyntax Node)>();

            foreach (XmlNodeSyntax node in doc.Content)
            {
                string? tagName = GetTopLevelTagName(node);

                if (tagName == null)
                {
                    continue;
                }

                int order = GetTopLevelTagOrder(tagName);

                if (order < 0)
                {
                    continue;
                }

                orderedTags.Add((tagName, node));
            }

            if (orderedTags.Count == 0)
            {
                return;
            }

            if (HasInvalidRemarksPlacement(orderedTags, out XmlNodeSyntax? invalidRemarksNode))
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    "remarks",
                    XmlDocSmells.TopLevelTagOrderMismatch,
                    invalidRemarksNode!.SpanStart));

                return;
            }

            int highestSeenOrder = -1;

            foreach ((string TagName, XmlNodeSyntax Node) entry in orderedTags)
            {
                if (entry.TagName == "remarks")
                {
                    continue;
                }

                int order = GetTopLevelTagOrder(entry.TagName);

                if (order < highestSeenOrder)
                {
                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        entry.TagName,
                        XmlDocSmells.TopLevelTagOrderMismatch,
                        entry.Node.SpanStart));

                    return;
                }

                if (order > highestSeenOrder)
                {
                    highestSeenOrder = order;
                }
            }
        }

        /// <summary>
        /// Determines whether a top-level <c>remarks</c> tag is placed at an invalid position.
        /// </summary>
        /// <param name="orderedTags">The ordered top-level documentation tags.</param>
        /// <param name="invalidRemarksNode">
        /// The offending remarks node if an invalid placement is found; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a remarks tag is placed at an invalid position; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidRemarksPlacement(
            List<(string TagName, XmlNodeSyntax Node)> orderedTags,
            out XmlNodeSyntax? invalidRemarksNode)
        {
            List<int> remarksIndices = new List<int>();

            for (int i = 0; i < orderedTags.Count; i++)
            {
                if (orderedTags[i].TagName == "remarks")
                {
                    remarksIndices.Add(i);
                }
            }

            invalidRemarksNode = null;

            if (remarksIndices.Count == 0)
            {
                return false;
            }

            foreach (int remarksIndex in remarksIndices)
            {
                bool isDirectlyAfterSummary = IsRemarksDirectlyAfterSummary(orderedTags, remarksIndex);
                bool isAtEndOrDirectlyBeforeSeeAlso = IsRemarksAtEndOrDirectlyBeforeSeeAlso(orderedTags, remarksIndex);

                if (isDirectlyAfterSummary || isAtEndOrDirectlyBeforeSeeAlso)
                {
                    continue;
                }

                invalidRemarksNode = orderedTags[remarksIndex].Node;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the remarks tag is placed directly after the summary tag.
        /// </summary>
        /// <param name="orderedTags">The ordered top-level documentation tags.</param>
        /// <param name="remarksIndex">The index of the remarks tag.</param>
        /// <returns>
        /// <see langword="true"/> if the remarks tag is directly after summary; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRemarksDirectlyAfterSummary(
            List<(string TagName, XmlNodeSyntax Node)> orderedTags,
            int remarksIndex)
        {
            if (remarksIndex != 1)
            {
                return false;
            }

            return orderedTags[0].TagName == "summary";
        }

        /// <summary>
        /// Determines whether the remarks tag is placed at the end or directly before the first seealso tag.
        /// </summary>
        /// <param name="orderedTags">The ordered top-level documentation tags.</param>
        /// <param name="remarksIndex">The index of the remarks tag.</param>
        /// <returns>
        /// <see langword="true"/> if the remarks tag is at the end or directly before the first seealso tag;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRemarksAtEndOrDirectlyBeforeSeeAlso(
            List<(string TagName, XmlNodeSyntax Node)> orderedTags,
            int remarksIndex)
        {
            int firstSeeAlsoIndex = -1;

            for (int i = 0; i < orderedTags.Count; i++)
            {
                if (orderedTags[i].TagName == "seealso")
                {
                    firstSeeAlsoIndex = i;
                    break;
                }
            }

            if (firstSeeAlsoIndex >= 0)
            {
                return remarksIndex == firstSeeAlsoIndex - 1;
            }

            return remarksIndex == orderedTags.Count - 1;
        }

        /// <summary>
        /// Gets the local tag name of a top-level XML documentation node.
        /// </summary>
        /// <param name="node">The XML node to inspect.</param>
        /// <returns>
        /// The local tag name if the node is an XML element; otherwise <see langword="null"/>.
        /// </returns>
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
        /// Gets the expected relative order of a top-level XML documentation tag.
        /// </summary>
        /// <param name="tagName">The XML tag name.</param>
        /// <returns>
        /// A non-negative order value for supported tags; otherwise <c>-1</c>.
        /// </returns>
        private static int GetTopLevelTagOrder(string tagName)
        {
            return tagName switch
            {
                "summary" => 10,
                "typeparam" => 20,
                "param" => 30,
                "returns" => 40,
                "value" => 40,
                "exception" => 50,
                "seealso" => 60,
                "remarks" => 70,
                _ => -1
            };
        }
    }
}
