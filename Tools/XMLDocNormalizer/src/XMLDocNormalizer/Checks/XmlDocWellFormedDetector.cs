using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

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
        /// Marker tag name used for syntactically invalid XML documentation tags.
        /// </summary>
        private const string InvalidTagName = "<invalid-xml-tag>";

        /// <summary>
        /// Regex that matches any angle-bracket tag-like token in XML doc raw text.
        /// </summary>
        private static readonly Regex AnyTagRegex = new(
            @"<(?<body>[^>]*)>",
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
                AddInvalidTagFindingsFromRawText(tree, findings, filePath, doc);
                AddMissingEndTagFindingsFromRawText(tree, findings, filePath, doc);

                IEnumerable<XmlElementSyntax> elements =
                    doc.DescendantNodes()
                        .OfType<XmlElementSyntax>();

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    if (string.IsNullOrWhiteSpace(tagName))
                    {
                        // Roslyn produced an element with no usable name (malformed XML). DOC115 is reported via raw text scan.
                        continue;
                    }

                    // Unknown or misspelled XML doc tag.
                    if (!XmlDocTagDefinitions.KnownTags.Contains(tagName))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.UnknownTag,
                            element.Span,
                            snippet: SyntaxUtils.GetSnippet(element),
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
                            snippet: SyntaxUtils.GetSnippet(element)));
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
                            snippet: SyntaxUtils.GetSnippet(element)));
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
                            snippet: SyntaxUtils.GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "param"
                        && !SyntaxUtils.HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.ParamMissingName,
                            element.Span,
                            snippet: SyntaxUtils.GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "typeparam"
                        && !SyntaxUtils.HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.TypeParamMissingName,
                            element.Span,
                            snippet: SyntaxUtils.GetSnippet(element)));
                        continue;
                    }

                    if (tagName == "exception"
                        && !SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
                    {
                        findings.Add(FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            tagName,
                            XmlDocSmells.ExceptionMissingCref,
                            element.Span,
                            snippet: SyntaxUtils.GetSnippet(element)));
                        continue;
                    }
                }
            }

            return findings;
        }

        /// <summary>
        /// Adds findings for syntactically invalid XML doc tags by scanning the raw documentation comment text.
        /// </summary>
        /// <remarks>
        /// This is a robustness fallback for cases where Roslyn cannot construct a well-formed <see cref="XmlElementSyntax"/>
        /// (e.g. tags like <c>&lt;&gt;</c> or <c>&lt;:&gt;</c>). In those cases, the element name may be empty and would
        /// otherwise lead to crashes or lost diagnostics.
        /// </remarks>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment trivia to scan.</param>
        private static void AddInvalidTagFindingsFromRawText(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            DocumentationCommentTriviaSyntax doc)
        {
            string raw = doc.ToFullString();

            foreach (Match match in AnyTagRegex.Matches(raw))
            {
                string body = match.Groups["body"].Value;

                // Ignore closing tags, comments, and processing instructions.
                string trimmed = body.TrimStart();
                if (trimmed.StartsWith("/", StringComparison.Ordinal) ||
                    trimmed.StartsWith("!", StringComparison.Ordinal) ||
                    trimmed.StartsWith("?", StringComparison.Ordinal))
                {
                    continue;
                }

                // Extract a candidate "name" (up to whitespace or '/')
                int end = 0;
                while (end < trimmed.Length && !char.IsWhiteSpace(trimmed[end]) && trimmed[end] != '/')
                {
                    end++;
                }

                string name = end > 0 ? trimmed.Substring(0, end) : string.Empty;

                // Valid XML name must start with letter or underscore.
                if (string.IsNullOrWhiteSpace(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
                {
                    int absolutePos = doc.FullSpan.Start + match.Index;

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        InvalidTagName,
                        XmlDocSmells.InvalidXmlTag,
                        absolutePos,
                        snippet: string.Empty,
                        match.Value));
                }
            }
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
    }
}
