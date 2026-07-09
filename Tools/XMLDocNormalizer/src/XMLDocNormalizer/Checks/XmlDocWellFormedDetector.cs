using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects malformed and structurally invalid XML documentation tags in a syntax tree.
    /// </summary>
    internal static class XmlDocWellFormedDetector
    {
        /// <summary>
        /// Marker tag name used for syntactically invalid XML documentation tags.
        /// </summary>
        private const string InvalidTagName = "<invalid-xml-tag>";

        /// <summary>
        /// Matches XML start tags that are not closing tags.
        /// </summary>
        /// <remarks>
        /// This expression is used as a fallback for malformed XML where Roslyn may not produce a complete XML element node.
        /// </remarks>
        private static readonly Regex StartTagRegex = new(
            @"<(?!/)(?<name>[A-Za-z_][A-Za-z0-9_\-\.]*)\b(?<attrs>[^>]*)>",
            RegexOptions.Compiled);

        /// <summary>
        /// Matches any angle-bracket tag-like token in raw XML documentation text.
        /// </summary>
        private static readonly Regex AnyTagRegex = new(
            @"<(?<body>[^>]*)>",
            RegexOptions.Compiled);

        /// <summary>
        /// Scans the syntax tree and returns findings for malformed and structurally invalid XML documentation tags.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A list of findings.
        /// </returns>
        public static List<Finding> FindMalformedTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<DocumentationCommentTriviaSyntax> docTrivias =
                root.DescendantTrivia(descendIntoTrivia: true)
                    .Select(trivia => trivia.GetStructure())
                    .OfType<DocumentationCommentTriviaSyntax>();

            foreach (DocumentationCommentTriviaSyntax doc in docTrivias)
            {
                AddInvalidTagFindingsFromRawText(tree, findings, filePath, doc);
                AddMissingEndTagFindingsFromRawText(tree, findings, filePath, doc);
                AddStructuredTagFindings(tree, findings, filePath, doc);
            }

            return findings;
        }

        /// <summary>
        /// Adds findings for structured XML elements that Roslyn could parse.
        /// </summary>
        /// <param name="tree">The syntax tree containing the documentation comment.</param>
        /// <param name="findings">The collection to which findings will be added.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="doc">The documentation comment trivia to inspect.</param>
        private static void AddStructuredTagFindings(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            DocumentationCommentTriviaSyntax doc)
        {
            IEnumerable<XmlElementSyntax> elements = XmlDocElementQuery.Elements(doc);

            foreach (XmlElementSyntax element in elements)
            {
                string tagName = element.StartTag.Name.LocalName.Text;

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    continue;
                }

                if (!XmlDocTagDefinitions.KnownTags.Contains(tagName))
                {
                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.UnknownTag,
                        element.Span,
                        CreateContext(doc, "XmlTag", tagName, filePath),
                        snippet: SyntaxUtils.GetSnippet(element),
                        tagName));

                    continue;
                }

                if (element.EndTag == null)
                {
                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.MissingEndTag,
                        element.Span,
                        CreateContext(doc, "XmlTag", tagName, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }

                if (tagName == "paramref")
                {
                    string? targetName = XmlDocTagExtraction.TryGetNameAttributeValue(element);

                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.ParamRefNotEmpty,
                        element.Span,
                        CreateContext(doc, "ParamRefTag", targetName, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }

                if (tagName == "typeparamref")
                {
                    string? targetName = XmlDocTagExtraction.TryGetNameAttributeValue(element);

                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.TypeParamRefNotEmpty,
                        element.Span,
                        CreateContext(doc, "TypeParamRefTag", targetName, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }

                if (tagName == "param" && !SyntaxUtils.HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                {
                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.ParamMissingName,
                        element.Span,
                        CreateContext(doc, "ParameterTag", targetName: null, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }

                if (tagName == "typeparam" && !SyntaxUtils.HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                {
                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.TypeParamMissingName,
                        element.Span,
                        CreateContext(doc, "TypeParameterTag", targetName: null, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }

                if (tagName == "exception" && !SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
                {
                    findings.Add(FindingFactory.AtSpanStart(
                        tree,
                        filePath,
                        tagName,
                        XmlDocSmells.ExceptionMissingCref,
                        element.Span,
                        CreateContext(doc, "ExceptionTag", targetName: null, filePath),
                        snippet: SyntaxUtils.GetSnippet(element)));

                    continue;
                }
            }
        }

        /// <summary>
        /// Adds findings for syntactically invalid XML documentation tags by scanning the raw documentation comment text.
        /// </summary>
        /// <remarks>
        /// This is a robustness fallback for cases where Roslyn cannot construct a well-formed XML element node.
        /// In those cases, the element name may be empty and would otherwise lead to crashes or lost diagnostics.
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
                string trimmed = body.TrimStart();

                if (trimmed.StartsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (trimmed.StartsWith("!", StringComparison.Ordinal))
                {
                    continue;
                }

                if (trimmed.StartsWith("?", StringComparison.Ordinal))
                {
                    continue;
                }

                int end = 0;

                while (end < trimmed.Length && !char.IsWhiteSpace(trimmed[end]) && trimmed[end] != '/')
                {
                    end++;
                }

                string name;

                if (end > 0)
                {
                    name = trimmed.Substring(0, end);
                }
                else
                {
                    name = string.Empty;
                }

                if (!IsValidTagNameStart(name))
                {
                    int absolutePos = doc.FullSpan.Start + match.Index;

                    findings.Add(FindingFactory.AtPosition(
                        tree,
                        filePath,
                        InvalidTagName,
                        XmlDocSmells.InvalidXmlTag,
                        absolutePos,
                        CreateContext(doc, "XmlTag", match.Value, filePath),
                        snippet: string.Empty,
                        match.Value));
                }
            }
        }

        /// <summary>
        /// Adds findings for missing end tags by scanning the raw documentation comment text.
        /// </summary>
        /// <remarks>
        /// This fallback is required because malformed XML often does not produce a complete XML element node in Roslyn's syntax tree.
        /// </remarks>
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
            string raw = doc.ToFullString();

            foreach (Match match in StartTagRegex.Matches(raw))
            {
                string tagName = match.Groups["name"].Value;

                if (!XmlDocTagDefinitions.ContainerTags.Contains(tagName))
                {
                    continue;
                }

                if (match.Value.EndsWith("/>", StringComparison.Ordinal))
                {
                    continue;
                }

                string endTag = "</" + tagName + ">";
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
                    absolutePos,
                    CreateContext(doc, "XmlTag", tagName, filePath)));
            }
        }

        /// <summary>
        /// Determines whether the extracted XML tag name starts with a valid XML-name start character.
        /// </summary>
        /// <param name="name">The extracted candidate tag name.</param>
        /// <returns>
        /// True if the name starts with a letter or underscore; otherwise false.
        /// </returns>
        private static bool IsValidTagNameStart(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (char.IsLetter(name[0]))
            {
                return true;
            }

            if (name[0] == '_')
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates finding context metadata for a documentation comment.
        /// </summary>
        /// <param name="doc">The documentation comment that owns the finding.</param>
        /// <param name="subjectKind">The documentation subject kind.</param>
        /// <param name="targetName">The affected target name if one exists.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>
        /// A populated finding context.
        /// </returns>
        private static FindingContext CreateContext(
            DocumentationCommentTriviaSyntax doc,
            string subjectKind,
            string? targetName,
            string filePath)
        {
            return FindingContextBuilder.ForDocumentationComment(
                doc,
                subjectKind,
                targetName: targetName,
                filePath: filePath);
        }
    }
}
