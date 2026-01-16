using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Rewriting
{
    /// <summary>
    /// Rewriter that normalizes the C# literals true, false and null
    /// inside XML documentation comments to plain text.
    /// </summary>
    /// <remarks>
    /// The rewriter removes XML constructs such as see langword, c and code
    /// elements and replaces them with their literal text representation.
    ///
    /// The original casing of the literals is preserved.
    ///
    /// Only documentation comments introduced with '///' are processed.
    /// Comments written using '/** ... */' are ignored.
    /// </remarks>
    internal sealed class LiteralRefactorer : CSharpSyntaxRewriter
    {
        /// <summary>
        /// Regular expression used to detect the literals true, false and null
        /// as standalone words in text nodes.
        /// </summary>
        private static readonly Regex literalRegex =
            new Regex(@"\b(true|false|null)\b",
                      RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Visits a syntax trivia and rewrites XML documentation comments
        /// that contain literal references.
        /// </summary>
        /// <param name="trivia">The trivia to visit.</param>
        /// <returns>
        /// The rewritten trivia if it represents a documentation comment;
        /// otherwise, the original trivia.
        /// </returns>
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                return trivia;
            }

            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax doc)
            {
                return trivia;
            }

            List<XmlNodeSyntax> newContent = doc.Content
                .SelectMany(ReplaceLiteralsInNode)
                .ToList();

            return SyntaxFactory.Trivia(
                doc.WithContent(SyntaxFactory.List(newContent)));
        }

        /// <summary>
        /// Rewrites a single XML node and replaces any occurrence of
        /// true, false or null with plain text.
        /// </summary>
        /// <param name="node">The XML node to process.</param>
        /// <returns>
        /// A sequence of XML nodes representing the rewritten content.
        /// Multiple nodes may be returned when a text node contains
        /// multiple literal occurrences.
        /// </returns>
        private IEnumerable<XmlNodeSyntax> ReplaceLiteralsInNode(XmlNodeSyntax node)
        {
            // 1) Plain text nodes: keep as-is (no keyword splitting/normalization here).
            if (node is XmlTextSyntax)
            {
                yield return node;
                yield break;
            }

            // 2) <see langword="..."/> -> convert to plain text (e.g. null/true/false).
            if (node is XmlEmptyElementSyntax emptyElement)
            {
                if (emptyElement.Name.LocalName.Text == "see")
                {
                    XmlTextAttributeSyntax? langwordAttribute =
                        emptyElement.Attributes
                            .OfType<XmlTextAttributeSyntax>()
                            .FirstOrDefault(a => a.Name.LocalName.Text == "langword");

                    if (langwordAttribute != null)
                    {
                        yield return SyntaxFactory.XmlText(
                            langwordAttribute.TextTokens.ToFullString());
                        yield break;
                    }
                }
            }

            // 3) <c>...</c> and <code>...</code> -> always expand to plain text,
            // preserving the original casing and text exactly as written.
            if (node is XmlElementSyntax element)
            {
                string elementName = element.StartTag.Name.LocalName.Text;

                // <c>...</c> -> always unwrap.
                if (elementName == "c")
                {
                    // If <c> contains nested elements, flatten it by returning its (processed) children.
                    if (!IsTextOnly(element))
                    {
                        foreach (XmlNodeSyntax child in element.Content.SelectMany(ReplaceLiteralsInNode))
                        {
                            yield return child;
                        }

                        yield break;
                    }

                    string raw = ExtractXmlTextValueText(element);
                    string withoutExterior = RemoveDocExterior(raw);
                    string cleaned = TrimDocTextPreserveNewlines(withoutExterior);

                    yield return SyntaxFactory.XmlText(cleaned);
                    yield break;
                }

                // <code>...</code> -> expand ONLY if it contains exactly one token after whitespace normalization.
                // Otherwise keep the <code> block unchanged.
                if (elementName == "code")
                {
                    // If <code> contains nested elements, flatten it by returning its (processed) children.
                    if (!IsTextOnly(element))
                    {
                        foreach (XmlNodeSyntax child in element.Content.SelectMany(ReplaceLiteralsInNode))
                        {
                            yield return child;
                        }

                        yield break;
                    }

                    // Empty <code></code> should disappear (becoming empty text).
                    string raw = ExtractXmlTextValueText(element);
                    string withoutExterior = RemoveDocExterior(raw);
                    string normalized = NormalizeWhitespace(withoutExterior);
                    if (normalized.Length == 0)
                    {
                        yield return SyntaxFactory.XmlText(string.Empty);
                        yield break;
                    }

                    // Unwrap <code> ONLY if it contains exactly one token after whitespace normalization.
                    if (TryGetSingleTokenFromCode(element, out string token))
                    {
                        yield return SyntaxFactory.XmlText(token);
                        yield break;
                    }

                    // Otherwise keep the <code> block unchanged.
                    yield return node;
                    yield break;
                }


                // 4) Recursively process child nodes of all other elements.
                List<XmlNodeSyntax> newChildren =
                    element.Content
                        .SelectMany(ReplaceLiteralsInNode)
                        .ToList();

                yield return element.WithContent(SyntaxFactory.List(newChildren));
                yield break;
            }

            // 5) Fallback: return node unchanged.
            yield return node;
        }

        /// <summary>
        /// Determines whether a code-Tag element contains only a single token.
        /// Newlines and other whitespace are treated as separators.
        /// </summary>
        /// <param name="element">The code-tag element.</param>
        /// <param name="token">The extracted token if the element contains exactly one token.</param>
        /// <returns>True if the code element should be unwrapped; otherwise, false.</returns>
        private static bool TryGetSingleTokenFromCode(XmlElementSyntax element, out string token)
        {
            string raw = ExtractXmlTextValueText(element);
            // Remove doc-comment exterior text that Roslyn may include in XmlText tokens for multi-line docs.
            string withoutExterior = RemoveDocExterior(raw);
            // Normalize all whitespace (spaces, tabs, newlines) to single spaces.
            string normalized = NormalizeWhitespace(withoutExterior);

            if (normalized.Length == 0)
            {
                token = string.Empty;
                return false;
            }

            // Split by spaces; if exactly one piece remains, it is a single token.
            string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                token = parts[0];
                return true;
            }

            token = string.Empty;
            return false;
        }

        /// <summary>
        /// Extracts the textual content of an XML element using token value text,
        /// which is more stable than <see cref="SyntaxNode.ToFullString"/> for doc comments.
        /// </summary>
        /// <param name="element">The element to extract text from.</param>
        /// <returns>The concatenated text content.</returns>
        private static string ExtractXmlTextValueText(XmlElementSyntax element)
        {
            StringBuilder sb = new();

            foreach (XmlNodeSyntax node in element.Content)
            {
                if (node is XmlTextSyntax text)
                {
                    foreach (SyntaxToken tt in text.TextTokens)
                    {
                        sb.Append(tt.ValueText);
                    }
                }
                else
                {
                    // Preserve non-text nodes if any (rare inside <code>, but safe).
                    sb.Append(node.ToFullString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes leading doc-comment exterior markers (e.g. "///") from each line of text.
        /// </summary>
        /// <param name="text">The raw text which may include doc-comment exterior.</param>
        /// <returns>The text without doc-comment exterior markers.</returns>
        private static string RemoveDocExterior(string text)
        {
            string normalized = text.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Trim indentation but keep inner spaces after the exterior.
                string trimmedStart = line.TrimStart();

                if (trimmedStart.StartsWith("///", StringComparison.Ordinal))
                {
                    string rest = trimmedStart.Substring(3);

                    // Optional single space right after "///".
                    if (rest.StartsWith(" ", StringComparison.Ordinal))
                    {
                        rest = rest.Substring(1);
                    }

                    lines[i] = rest;
                    continue;
                }

                // Some Roslyn tokenizations may leave the exterior in the middle of the line.
                // If it looks like an exterior-only line, drop it.
                if (trimmedStart == "///")
                {
                    lines[i] = string.Empty;
                    continue;
                }

                lines[i] = line;
            }

            return string.Join("\n", lines);
        }


        /// <summary>
        /// Collapses all whitespace (including newlines) to single spaces and trims the result.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>The normalized text.</returns>
        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder sb = new(text.Length);
            bool previousWasWhitespace = false;

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!previousWasWhitespace)
                    {
                        sb.Append(' ');
                        previousWasWhitespace = true;
                    }
                    continue;
                }

                sb.Append(c);
                previousWasWhitespace = false;
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Returns true if the element contains only <see cref="XmlTextSyntax"/> nodes.
        /// </summary>
        private static bool IsTextOnly(XmlElementSyntax element)
        {
            return element.Content.All(n => n is XmlTextSyntax);
        }

        /// <summary>
        /// Trims doc text while preserving newlines:
        /// - Normalizes line endings to \n
        /// - Trims each line
        /// - Removes leading/trailing empty lines
        /// </summary>
        private static string TrimDocTextPreserveNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r\n", "\n");
            List<string> lines = normalized.Split('\n').Select(l => l.Trim()).ToList();

            while (lines.Count > 0 && lines[0].Length == 0)
            {
                lines.RemoveAt(0);
            }

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines);
        }

    }
}
