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
    /// Detects syntax-based smells for <c>see</c> and <c>seealso</c> XML documentation tags.
    /// </summary>
    internal static class XmlDocSeeDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for <c>see</c> and <c>seealso</c> syntax smells.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
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
        /// Analyzes a documentation comment for <c>see</c> and <c>seealso</c> elements.
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
                    node,
                    findings,
                    isTopLevel: true,
                    seeAlsoTargets);
            }

            DetectDuplicateSeeAlsoTargets(tree, filePath, findings, seeAlsoTargets);
        }

        /// <summary>
        /// Recursively analyzes an XML documentation node and its children.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="node">The current XML node to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">
        /// <see langword="true"/> if the node is a direct child of the documentation comment;
        /// otherwise <see langword="false"/>.
        /// </param>
        /// <param name="seeAlsoTargets">The collected top-level <c>seealso</c> targets for duplicate detection.</param>
        private static void AnalyzeXmlNode(
            SyntaxTree tree,
            string filePath,
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
                    element,
                    findings,
                    isTopLevel,
                    seeAlsoTargets);

                foreach (XmlNodeSyntax childNode in element.Content)
                {
                    AnalyzeXmlNode(
                        tree,
                        filePath,
                        childNode,
                        findings,
                        isTopLevel: false,
                        seeAlsoTargets);
                }
            }
        }

        /// <summary>
        /// Analyzes an empty XML documentation element such as <c>&lt;see /&gt;</c>.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="element">The XML empty element to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">
        /// <see langword="true"/> if the element is a direct child of the documentation comment;
        /// otherwise <see langword="false"/>.
        /// </param>
        /// <param name="seeAlsoTargets">The collected top-level <c>seealso</c> targets for duplicate detection.</param>
        private static void AnalyzeEmptyElement(
            SyntaxTree tree,
            string filePath,
            XmlEmptyElementSyntax element,
            List<Finding> findings,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            string tagName = SyntaxUtils.GetLocalName(element);

            if (tagName == "see")
            {
                ReportSeeFindings(tree, filePath, element, findings);
                return;
            }

            if (tagName == "seealso")
            {
                RegisterSeeAlsoTarget(element, isTopLevel, seeAlsoTargets);
                ReportSeeAlsoFindings(tree, filePath, element, findings, isTopLevel);
            }
        }

        /// <summary>
        /// Analyzes a non-empty XML documentation element such as <c>&lt;see&gt;...&lt;/see&gt;</c>.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="element">The XML element to analyze.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">
        /// <see langword="true"/> if the element is a direct child of the documentation comment;
        /// otherwise <see langword="false"/>.
        /// </param>
        /// <param name="seeAlsoTargets">The collected top-level <c>seealso</c> targets for duplicate detection.</param>
        private static void AnalyzeElement(
            SyntaxTree tree,
            string filePath,
            XmlElementSyntax element,
            List<Finding> findings,
            bool isTopLevel,
            Dictionary<string, List<XmlNodeSyntax>> seeAlsoTargets)
        {
            string tagName = SyntaxUtils.GetLocalName(element);

            if (tagName == "see")
            {
                ReportSeeFindings(tree, filePath, element, findings);

                if (XmlDocUtils.HasMeaningfulContent(element))
                {
                    AddFinding(
                        tree,
                        filePath,
                        "see",
                        XmlDocSmells.SeeNotEmpty,
                        element,
                        findings);
                }

                return;
            }

            if (tagName == "seealso")
            {
                RegisterSeeAlsoTarget(element, isTopLevel, seeAlsoTargets);
                ReportSeeAlsoFindings(tree, filePath, element, findings, isTopLevel);

                if (XmlDocUtils.HasMeaningfulContent(element))
                {
                    AddFinding(
                        tree,
                        filePath,
                        "seealso",
                        XmlDocSmells.SeeAlsoNotEmpty,
                        element,
                        findings);
                }
            }
        }

        /// <summary>
        /// Reports all syntax findings for an empty or non-empty <c>see</c> tag.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void ReportSeeFindings(
            SyntaxTree tree,
            string filePath,
            XmlNodeSyntax element,
            List<Finding> findings)
        {
            if (!XmlSeeTagRules.HasValidSeeTarget(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "see",
                    XmlDocSmells.SeeMissingTarget,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeTargetCombination(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "see",
                    XmlDocSmells.InvalidSeeAttributeCombination,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAttribute(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "see",
                    XmlDocSmells.InvalidSeeAttribute,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeHref(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "see",
                    XmlDocSmells.InvalidSeeHref,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeLangword(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "see",
                    XmlDocSmells.InvalidSeeLangword,
                    element,
                    findings);
            }
        }

        /// <summary>
        /// Reports all syntax findings for an empty or non-empty <c>seealso</c> tag.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="isTopLevel">
        /// <see langword="true"/> if the element is a direct child of the documentation comment;
        /// otherwise <see langword="false"/>.
        /// </param>
        private static void ReportSeeAlsoFindings(
            SyntaxTree tree,
            string filePath,
            XmlNodeSyntax element,
            List<Finding> findings,
            bool isTopLevel)
        {
            if (!XmlSeeTagRules.HasValidSeeAlsoTarget(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.SeeAlsoMissingTarget,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoTargetCombination(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.InvalidSeeAlsoAttributeCombination,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasSeeAlsoLangwordAttribute(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.SeeAlsoLangwordNotSupported,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoAttribute(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.InvalidSeeAlsoAttribute,
                    element,
                    findings);
            }

            if (XmlSeeTagRules.HasInvalidSeeAlsoHref(element))
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.InvalidSeeAlsoHref,
                    element,
                    findings);
            }

            if (!isTopLevel)
            {
                AddFinding(
                    tree,
                    filePath,
                    "seealso",
                    XmlDocSmells.SeeAlsoNotTopLevel,
                    element,
                    findings);
            }
        }

        /// <summary>
        /// Registers the target of a top-level <c>seealso</c> element for duplicate detection.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="isTopLevel">
        /// <see langword="true"/> if the element is a direct child of the documentation comment;
        /// otherwise <see langword="false"/>.
        /// </param>
        /// <param name="seeAlsoTargets">The collected top-level <c>seealso</c> targets.</param>
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
        /// Reports duplicate top-level <c>seealso</c> targets within the same documentation comment.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="findings">The findings collection to append to.</param>
        /// <param name="seeAlsoTargets">The collected top-level <c>seealso</c> targets.</param>
        private static void DetectDuplicateSeeAlsoTargets(
            SyntaxTree tree,
            string filePath,
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

                foreach (XmlNodeSyntax node in nodes)
                {
                    AddFinding(
                        tree,
                        filePath,
                        "seealso",
                        XmlDocSmells.DuplicateSeeAlsoTarget,
                        node,
                        findings);
                }
            }
        }

        /// <summary>
        /// Adds a finding at the start of the specified node.
        /// </summary>
        /// <param name="tree">The syntax tree used for reporting.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The XML tag name.</param>
        /// <param name="smell">The smell metadata.</param>
        /// <param name="node">The node that caused the finding.</param>
        /// <param name="findings">The findings collection to append to.</param>
        private static void AddFinding(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            SyntaxNode node,
            List<Finding> findings)
        {
            findings.Add(
                FindingFactory.AtSpanStart(
                    tree,
                    filePath,
                    tagName,
                    smell,
                    node.Span,
                    SyntaxUtils.GetSnippet(node)));
        }
    }
}
