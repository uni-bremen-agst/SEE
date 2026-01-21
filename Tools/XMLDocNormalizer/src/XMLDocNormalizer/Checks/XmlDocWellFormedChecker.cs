using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using XMLDocNormalizer.Models;
using static XMLDocNormalizer.Checks.XmlDocHelpers;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects malformed XML documentation tags in a syntax tree.
    /// </summary>
    internal static class XmlDocWellFormedChecker
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
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            XmlDocSmells.UnknownTag,
                            tagName);
                        continue;
                    }

                    // Missing end tag.
                    if (element.EndTag == null)
                    {
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            XmlDocSmells.MissingEndTag);
                        continue;
                    }

                    // Common mistake: <paramref> and <typeparamref> should be empty elements (<.../>).
                    if (tagName == "paramref" || tagName == "typeparamref")
                    {
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            XmlDocSmells.ParamRefNotEmpty);
                        continue;
                    }

                    if (tagName == "param" && !HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            XmlDocSmells.ParamMissingName);
                        continue;
                    }

                    if (tagName == "exception" && !HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
                    {
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            XmlDocSmells.ExceptionMissingCref);
                        continue;
                    }
                }
            }

            return findings;
        }

        /// <summary>
        /// Creates and adds a <see cref="Finding"/> for a malformed XML documentation element.
        /// </summary>
        /// <param name="tree">The syntax tree containing the node.</param>
        /// <param name="findings">The collection to which the finding will be added.</param>
        /// <param name="filePath">The source file path.</param>
        /// <param name="node">The syntax node representing the problematic XML element.</param>
        /// <param name="tagName">The XML tag name associated with the finding.</param>
        /// <param name="smell">The documentation smell that describes the issue.</param>
        /// <param name="messageArgs">
        /// Optional arguments used to format the smell's message template
        /// (e.g. parameter name, exception type).
        /// </param>
        private static void AddFinding(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            SyntaxNode node,
            string tagName,
            XmlDocSmell smell,
            params object[] messageArgs)
        {
            FileLinePositionSpan span = tree.GetLineSpan(node.Span);
            int line = span.StartLinePosition.Line + 1;
            int column = span.StartLinePosition.Character + 1;

            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            findings.Add(new Finding(smell, filePath, tagName, line, column, snippet, messageArgs));
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
                AddFindingAtSpan(
                    tree,
                    findings,
                    filePath,
                    tagName,
                    XmlDocSmells.MissingEndTag,
                    absolutePos);
            }
        }

        /// <summary>
        /// Creates and adds a <see cref="Finding"/> for a specific absolute position
        /// within the syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree used to calculate line and column.</param>
        /// <param name="findings">The collection to which the finding will be added.</param>
        /// <param name="filePath">The source file path.</param>
        /// <param name="tagName">The XML tag name associated with the finding.</param>
        /// <param name="smell">The documentation smell that describes the issue.</param>
        /// <param name="absolutePosition">The absolute position within the file.</param>
        /// <param name="messageArgs">
        /// Optional arguments used to format the smell's message template.
        /// </param>
        private static void AddFindingAtSpan(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            int absolutePosition,
            params object[] messageArgs)
        {
            TextSpan span = new(absolutePosition, length: 1);
            FileLinePositionSpan lineSpan = tree.GetLineSpan(span);

            int line = lineSpan.StartLinePosition.Line + 1;
            int column = lineSpan.StartLinePosition.Character + 1;

            findings.Add(new Finding(
                smell,
                filePath,
                tagName,
                line,
                column,
                snippet: string.Empty,
                messageArgs));
        }

    }
}
