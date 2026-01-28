using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using static XMLDocNormalizer.Checks.XmlDocHelpers;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects malformed XML documentation tags in a syntax tree.
    /// </summary>
    internal static class XmlDocWellFormedDetector
    {
        /// <summary>
        /// Regex that matches XML start tags (non-closing).
        /// This is used as a fallback for malformed XML where Roslyn may not produce
        /// a complete <see cref="XmlElementSyntax"/> node.
        /// </summary>
        private static readonly Regex StartTagRegex = new(
            @"<(?!/)(?<name>[A-Za-z_][A-Za-z0-9_\-\.]*)\b(?<attrs>[^>]*)>",
            RegexOptions.Compiled);

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
                AddMissingEndTagFindingsFromRawText(tree, findings, filePath, doc);

                IEnumerable<XmlElementSyntax> elements =
                    doc.DescendantNodes()
                        .OfType<XmlElementSyntax>();

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    // Unknown or misspelled XML doc tag.
                    if (!XmlDocTagDefinitions.KnownTags.Contains(tagName))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.UnknownTag,
                            element.Span,
                            snippet: GetSnippet(element),
                            tagName));
                        continue;
                    }

                    // Missing end tag.
                    if (element.EndTag == null)
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.MissingEndTag,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }

                    // Common mistake: <paramref> should be empty elements (<.../>).
                    if (tagName == "paramref")
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.ParamRefNotEmpty,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "typeparamref")
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.TypeParamRefNotEmpty,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "param" && !HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.ParamMissingName,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "typeparam" && !HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.TypeParamMissingName,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "exception" && !HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.ExceptionMissingCref,
                            element.Span,
                            snippet: GetSnippet(element)));
                        continue;
                    }
                }
            }

            return findings;
        }

        /// <summary>
        /// Adds <see cref="Finding"/> entries for missing end tags by scanning the raw documentation comment text.
        /// This is required because malformed XML often does not produce a complete <see cref="XmlElementSyntax"/>
        /// node in Roslyn's syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment trivia to scan.</param>
        private static void AddMissingEndTagFindingsFromRawText(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            DocumentationCommentTriviaSyntax doc)
        {
            // doc.ToFullString() includes the leading "///" exteriors; this is good because
            // it makes the reported column align with what the user sees.
            string raw = doc.ToFullString();

            foreach (Match match in StartTagRegex.Matches(raw))
            {
                string tagName = match.Groups["name"].Value;

                // Only report missing end tags for tags that are expected to have an end tag.
                // Empty-element tags such as <paramref> and <typeparamref> must be ignored here.
                if (!XmlDocTagDefinitions.ContainerTags.Contains(tagName))
                {
                    continue;
                }

                // Ignore self-closing tags (e.g. <see .../>).
                if (match.Value.EndsWith("/>", StringComparison.Ordinal))
                {
                    continue;
                }

                // If there is no matching end tag later in the doc trivia, report MissingEndTag.
                string endTag = $"</{tagName}>";
                int endIndex = raw.IndexOf(endTag, match.Index + match.Length, StringComparison.Ordinal);
                if (endIndex >= 0)
                {
                    continue;
                }

                int absolutePos = doc.FullSpan.Start + match.Index;
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName,
                    XmlDocSmells.MissingEndTag,
                    absolutePos));
            }
        }

        /// <summary>
        /// Creates a short, single-line snippet for a syntax node that is suitable for console output.
        /// </summary>
        /// <param name="node">The node to create a snippet for.</param>
        /// <returns>A single-line snippet, truncated to a reasonable maximum length.</returns>
        private static string GetSnippet(SyntaxNode node)
        {
            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            return snippet;
        }
    }
}
