using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.See;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects syntax-based smells for see and seealso XML documentation tags.
    /// </summary>
    internal static class XmlDocSeeDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for see and seealso syntax smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings.
        /// </returns>
        public static List<Finding> FindSeeSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<DocumentationCommentTriviaSyntax> comments =
                root.DescendantNodes(descendIntoTrivia: true)
                    .OfType<DocumentationCommentTriviaSyntax>();

            foreach (DocumentationCommentTriviaSyntax comment in comments)
            {
                AnalyzeDocumentationComment(tree, filePath, comment, findings);
            }

            return findings;
        }

        /// <summary>
        /// Analyzes a documentation comment for see and seealso elements.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void AnalyzeDocumentationComment(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            List<Finding> findings)
        {
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets =
                new Dictionary<string, List<XmlNodeSyntax>>(StringComparer.Ordinal);

            foreach (XmlNodeSyntax node in comment.Content)
            {
                AnalyzeXmlNode(
                    tree,
                    filePath,
                    comment,
                    node,
                    findings,
                    isTopLevel: true,
                    seeAlsoTargets);
            }

            DetectDuplicateSeeAlsoTargets(tree, filePath, comment, findings, seeAlsoTargets);
        }

        /// <summary>
        /// Recursively analyzes an XML documentation node and its children.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the current XML node.</param>
        /// <param name="node">The current XML node to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">Indicates whether the node is a direct child of the documentation comment.</param>
        /// <param name="seeAlsoTargets">The collected top-level seealso targets for duplicate detection.</param>
        private static void AnalyzeXmlNode(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            XmlNodeSyntax node,
            List<Finding> findings,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            if (node is XmlEmptyElementSyntax emptyElement)
            {
                AnalyzeEmptyElement(
                    tree,
                    filePath,
                    comment,
                    emptyElement,
                    findings,
                    isTopLevel,
                    seeAlsoTargets);

                return;
            }

            if (node is XmlElementSyntax element)
            {
                AnalyzeElement(
                    tree,
                    filePath,
                    comment,
                    element,
                    findings,
                    isTopLevel,
                    seeAlsoTargets);

                foreach (XmlNodeSyntax childNode in element.Content)
                {
                    AnalyzeXmlNode(
                        tree,
                        filePath,
                        comment,
                        childNode,
                        findings,
                        isTopLevel: false,
                        seeAlsoTargets);
                }
            }
        }

        /// <summary>
        /// Analyzes an empty XML documentation element.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the XML element.</param>
        /// <param name="element">The XML empty element to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">Indicates whether the element is a direct child of the documentation comment.</param>
        /// <param name="seeAlsoTargets">The collected top-level seealso targets for duplicate detection.</param>
        private static void AnalyzeEmptyElement(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            XmlEmptyElementSyntax element,
            List<Finding> findings,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            string tagName = SyntaxUtils.GetLocalName(element);

            if (tagName == "see")
            {
                ReportSeeFindings(tree, filePath, comment, element, findings);
                return;
            }

            if (tagName == "seealso")
            {
                RegisterSeeAlsoTarget(element, isTopLevel, seeAlsoTargets);
                ReportSeeAlsoFindings(tree, filePath, comment, element, findings, isTopLevel);
            }
        }

        /// <summary>
        /// Analyzes a non-empty XML documentation element.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the XML element.</param>
        /// <param name="element">The XML element to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">Indicates whether the element is a direct child of the documentation comment.</param>
        /// <param name="seeAlsoTargets">The collected top-level seealso targets for duplicate detection.</param>
        private static void AnalyzeElement(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            XmlElementSyntax element,
            List<Finding> findings,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            string tagName = SyntaxUtils.GetLocalName(element);

            if (tagName == "see")
            {
                ReportSeeFindings(tree, filePath, comment, element, findings);

                if (XmlDocUtils.HasMeaningfulContent(element))
                {
                    AddFinding(
                        tree,
                        filePath,
                        comment,
                        "see",
                        "SeeTag",
                        XmlDocSmells.SeeNotEmpty,
                        element,
                        findings);
                }

                return;
            }

            if (tagName == "seealso")
            {
                RegisterSeeAlsoTarget(element, isTopLevel, seeAlsoTargets);
                ReportSeeAlsoFindings(tree, filePath, comment, element, findings, isTopLevel);

                if (XmlDocUtils.HasMeaningfulContent(element))
                {
                    AddFinding(
                        tree,
                        filePath,
                        comment,
                        "seealso",
                        "SeeAlsoTag",
                        XmlDocSmells.SeeAlsoNotEmpty,
                        element,
                        findings);
                }
            }
        }

        /// <summary>
        /// Reports all syntax findings for an empty or non-empty see tag.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the XML node.</param>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void ReportSeeFindings(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            XmlNodeSyntax element,
            List<Finding> findings)
        {
            if (!XmlSeeTagRules.HasValidSeeTarget(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "see",
                    "SeeTag",
                    XmlDocSmells.SeeMissingTarget,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeTargetCombination(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "see",
                    "SeeTag",
                    XmlDocSmells.InvalidSeeAttributeCombination,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAttribute(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "see",
                    "SeeTag",
                    XmlDocSmells.InvalidSeeAttribute,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeHref(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "see",
                    "SeeTag",
                    XmlDocSmells.InvalidSeeHref,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeLangword(element))
            {
                string invalidLangword = GetAttributeValue(element, "langword") ?? string.Empty;

                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "see",
                    "SeeTag",
                    XmlDocSmells.InvalidSeeLangword,
                    element,
                    findings,
                    null,
                    invalidLangword);
            }
        }

        /// <summary>
        /// Reports all syntax findings for an empty or non-empty seealso tag.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the XML node.</param>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">Indicates whether the element is a direct child of the documentation comment.</param>
        private static void ReportSeeAlsoFindings(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            XmlNodeSyntax element,
            List<Finding> findings,
            bool isTopLevel)
        {
            bool hasSeeAlsoLangwordAttribute = XmlSeeTagRules.HasSeeAlsoLangwordAttribute(element);

            if (!hasSeeAlsoLangwordAttribute && !XmlSeeTagRules.HasValidSeeAlsoTarget(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "seealso",
                    "SeeAlsoTag",
                    XmlDocSmells.SeeAlsoMissingTarget,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoTargetCombination(element))
                if (XmlSeeTagRules.HasInvalidSeeAlsoTargetCombination(element))
                {
                    AddFinding(
                        tree,
                        filePath,
                        comment,
                        "seealso",
                        "SeeAlsoTag",
                        XmlDocSmells.InvalidSeeAlsoAttributeCombination,
                        element,
                        findings);
                }

            if (hasSeeAlsoLangwordAttribute)
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "seealso",
                    "SeeAlsoTag",
                    XmlDocSmells.SeeAlsoLangwordNotSupported,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoAttribute(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "seealso",
                    "SeeAlsoTag",
                    XmlDocSmells.InvalidSeeAlsoAttribute,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoHref(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "seealso",
                    "SeeAlsoTag",
                    XmlDocSmells.InvalidSeeAlsoHref,
                    element,
                    findings);
            }

            if (!isTopLevel)
            {
                AddFinding(
                    tree,
                    filePath,
                    comment,
                    "seealso",
                    "SeeAlsoTag",
                    XmlDocSmells.SeeAlsoNotTopLevel,
                    element,
                    findings);
            }
        }

        /// <summary>
        /// Registers the target of a top-level seealso element for duplicate detection.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="isTopLevel">Indicates whether the element is a direct child of the documentation comment.</param>
        /// <param name="seeAlsoTargets">The collected top-level seealso targets.</param>
        private static void RegisterSeeAlsoTarget(
            XmlNodeSyntax element,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            if (!isTopLevel)
            {
                return;
            }

            string? target = XmlSeeTagRules.GetSeeAlsoTargetKey(element);

            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            if (!seeAlsoTargets.TryGetValue(target, out List<XmlNodeSyntax>? nodes))
            {
                nodes = new List<XmlNodeSyntax>();
                seeAlsoTargets.Add(target, nodes);
            }

            nodes.Add(element);
        }

        /// <summary>
        /// Reports duplicate top-level seealso targets within the same documentation comment.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the seealso nodes.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="seeAlsoTargets">The collected top-level seealso targets.</param>
        private static void DetectDuplicateSeeAlsoTargets(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            List<Finding> findings,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            foreach (KeyValuePair<string, List<XmlNodeSyntax>> pair in seeAlsoTargets)
            {
                List<XmlNodeSyntax> nodes = pair.Value;

                if (nodes.Count <= 1)
                {
                    continue;
                }

                for (int i = 1; i < nodes.Count; i++)
                {
                    XmlNodeSyntax duplicate = nodes[i];

                    AddFinding(
                        tree,
                        filePath,
                        comment,
                        "seealso",
                        "SeeAlsoTag",
                        XmlDocSmells.DuplicateSeeAlsoTarget,
                        duplicate,
                        findings,
                        targetName: pair.Key,
                        pair.Key);
                }
            }
        }

        /// <summary>
        /// Adds a finding at the start of the specified node.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="comment">The documentation comment that owns the node.</param>
        /// <param name="tagName">The XML tag name.</param>
        /// <param name="subjectKind">The documentation subject kind for context metadata.</param>
        /// <param name="smell">The smell metadata.</param>
        /// <param name="node">The node that caused the finding.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="targetName">The affected reference target if one exists.</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        private static void AddFinding(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            string tagName,
            string subjectKind,
            XmlDocSmell smell,
            SyntaxNode node,
            List<Finding> findings,
            string? targetName = null,
            params object[] messageArgs)
        {
            string? effectiveTargetName = targetName;

            if (effectiveTargetName == null && node is XmlNodeSyntax xmlNode)
            {
                effectiveTargetName = GetReferenceTarget(xmlNode);
            }

            FindingContext context = FindingContextBuilder.ForDocumentationComment(
                comment,
                subjectKind,
                targetName: effectiveTargetName,
                filePath: filePath);

            findings.Add(
                FindingFactory.AtSpanStart(
                    tree,
                    filePath,
                    tagName,
                    smell,
                    node.Span,
                    context,
                    snippet: SyntaxUtils.GetSnippet(node),
                    messageArgs));
        }

        /// <summary>
        /// Gets a stable reference target string from a see or seealso XML node.
        /// </summary>
        /// <param name="node">The XML node to inspect.</param>
        /// <returns>
        /// A target string if a cref, href, or langword attribute is present; otherwise null.
        /// </returns>
        private static string? GetReferenceTarget(XmlNodeSyntax node)
        {
            string? cref = GetAttributeValue(node, "cref");

            if (!string.IsNullOrWhiteSpace(cref))
            {
                return "cref:" + cref;
            }

            string? href = GetAttributeValue(node, "href");

            if (!string.IsNullOrWhiteSpace(href))
            {
                return "href:" + href;
            }

            string? langword = GetAttributeValue(node, "langword");

            if (!string.IsNullOrWhiteSpace(langword))
            {
                return "langword:" + langword;
            }

            return null;
        }

        /// <summary>
        /// Gets an attribute value from an XML node.
        /// </summary>
        /// <param name="node">The XML node to inspect.</param>
        /// <param name="attributeName">The local attribute name.</param>
        /// <returns>
        /// The attribute value if present; otherwise null.
        /// </returns>
        private static string? GetAttributeValue(XmlNodeSyntax node, string attributeName)
        {
            if (node is XmlElementSyntax element)
            {
                return SyntaxUtils.GetAttributeValue(element, attributeName);
            }

            if (node is XmlEmptyElementSyntax emptyElement)
            {
                return SyntaxUtils.GetAttributeValue(emptyElement, attributeName);
            }

            return null;
        }
    }
}
