using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Rewriting
{
    /// <summary>
    /// Helper class for normalizing XML documentation elements in C# code.
    /// </summary>
    internal static class XmlDocTagFormatter
    {
        /// <summary>
        /// XML tags for which capitalization should not be applied.
        /// </summary>
        private static readonly HashSet<string> NoCapitalize =
        [
            "see",
        "paramref",
        "c",
        "code"
        ];

        /// <summary>
        /// Normalizes an XML element by capitalizing the first letter and ensuring it ends with a period.
        /// </summary>
        /// <param name="element">The XML element to normalize.</param>
        /// <returns>The normalized XML element.</returns>
        public static XmlElementSyntax Normalize(XmlElementSyntax element)
        {
            List<XmlNodeSyntax> nodes = element.Content.ToList();
            nodes = Capitalize(nodes);
            nodes = EnsurePeriod(nodes);
            return element.WithContent(SyntaxFactory.List(nodes));
        }

        /// <summary>
        /// Capitalizes the first non-whitespace character of text nodes, except for certain tags.
        /// </summary>
        /// <param name="nodes">List of XML nodes.</param>
        /// <returns>Updated list of XML nodes with capitalization applied.</returns>
        private static List<XmlNodeSyntax> Capitalize(List<XmlNodeSyntax> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is XmlTextSyntax text)
                {
                    List<SyntaxToken> tokens = text.TextTokens.ToList();

                    for (int t = 0; t < tokens.Count; t++)
                    {
                        SyntaxToken tok = tokens[t];
                        if (string.IsNullOrWhiteSpace(tok.Text))
                            continue;

                        int idx = tok.Text.TakeWhile(char.IsWhiteSpace).Count();
                        if (idx >= tok.Text.Length)
                            continue;

                        char ch = tok.Text[idx];
                        if (char.IsLower(ch))
                        {
                            string newText =
                                tok.Text[..idx] +
                                char.ToUpperInvariant(ch) +
                                tok.Text[(idx + 1)..];

                            tokens[t] = SyntaxFactory.XmlTextLiteral(
                                tok.LeadingTrivia,
                                newText,
                                newText,
                                tok.TrailingTrivia);

                            nodes[i] = text.WithTextTokens(SyntaxFactory.TokenList(tokens));
                        }
                        return nodes;
                    }
                }

                if (nodes[i] is XmlEmptyElementSyntax e &&
                    NoCapitalize.Contains(e.Name.LocalName.Text))
                {
                    return nodes;
                }


                if (nodes[i] is XmlElementSyntax el &&
                    NoCapitalize.Contains(el.StartTag.Name.LocalName.Text))
                {
                    return nodes;
                }
            }

            return nodes;
        }

        /// <summary>
        /// Ensures that XML text nodes end with a period.
        /// </summary>
        /// <param name="nodes">List of XML nodes.</param>
        /// <returns>Updated list of XML nodes ending with a period.</returns>
        private static List<XmlNodeSyntax> EnsurePeriod(List<XmlNodeSyntax> nodes)
        {
            // Fast path: if the content already ends with a period, do nothing.
            // This prevents accidental double periods when Roslyn splits punctuation across nodes/tokens.
            string fullText = string.Concat(nodes.Select(n => n.ToFullString())).TrimEnd();
            if (fullText.EndsWith("."))
            {
                return nodes;
            }
            // Find the last "meaningful" node (ignore whitespace-only text nodes).
            int lastMeaningfulIndex = -1;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i] is XmlTextSyntax textNode)
                {
                    bool hasNonWhitespace = textNode.TextTokens.Any(t => !string.IsNullOrWhiteSpace(t.Text));
                    if (!hasNonWhitespace)
                    {
                        continue;
                    }
                }

                lastMeaningfulIndex = i;
                break;
            }

            if (lastMeaningfulIndex < 0)
            {
                nodes.Add(SyntaxFactory.XmlText("."));
                return nodes;
            }

            // Special case: if the content ends with a <para> element, do not append a period.
            // The paragraph content typically already carries its own punctuation, and adding a period
            // after </para> produces awkward output like "</para>.".
            if (nodes[lastMeaningfulIndex] is XmlElementSyntax lastElement)
            {
                if (lastElement.StartTag.Name.LocalName.Text == "para")
                {
                    return nodes;
                }
            }

            // Case 1: The content ends with an element (<see/>, <c>...</c>, etc.).
            // Append a period AFTER the element (before any trailing whitespace/newlines).
            if (nodes[lastMeaningfulIndex] is XmlEmptyElementSyntax ||
                nodes[lastMeaningfulIndex] is XmlElementSyntax)
            {
                // If there is a trailing whitespace text node after the element, prefix "." into it.
                if (lastMeaningfulIndex + 1 < nodes.Count && nodes[lastMeaningfulIndex + 1] is XmlTextSyntax wsText)
                {
                    List<SyntaxToken> wsTokens = wsText.TextTokens.ToList();
                    if (wsTokens.Count > 0)
                    {
                        SyntaxToken first = wsTokens[0];

                        // Put the period at the start of the trailing whitespace node,
                        // so it renders right after the element.
                        string newWs = "." + first.Text;

                        wsTokens[0] = SyntaxFactory.XmlTextLiteral(
                            first.LeadingTrivia,
                            newWs,
                            newWs,
                            first.TrailingTrivia);

                        nodes[lastMeaningfulIndex + 1] = wsText.WithTextTokens(SyntaxFactory.TokenList(wsTokens));
                        return nodes;
                    }
                }

                // Otherwise insert a standalone period node right after the element.
                nodes.Insert(lastMeaningfulIndex + 1, SyntaxFactory.XmlText("."));
                return nodes;
            }

            // Case 2: The content ends with text. Ensure that the LAST content line ends with a period.
            if (nodes[lastMeaningfulIndex] is XmlTextSyntax text)
            {
                List<SyntaxToken> tokens = text.TextTokens.ToList();

                for (int t = tokens.Count - 1; t >= 0; t--)
                {
                    SyntaxToken tok = tokens[t];
                    if (string.IsNullOrWhiteSpace(tok.Text))
                    {
                        continue;
                    }

                    string s = tok.Text;

                    // XmlText tokens may contain multiple documentation lines, e.g.:
                    // "component.\r\n            ///"
                    // We must ensure we punctuate the last CONTENT line, not the following "///" exterior line.
                    int lastNl = s.LastIndexOf('\n');
                    if (lastNl >= 0)
                    {
                        string beforeNl = s[..lastNl];

                        if (beforeNl.TrimEnd().EndsWith("."))
                        {
                            return nodes;
                        }

                        int insertPos = beforeNl.Length;
                        while (insertPos > 0 && char.IsWhiteSpace(beforeNl[insertPos - 1]))
                        {
                            insertPos--;
                        }

                        string newText = s.Insert(insertPos, ".");

                        tokens[t] = SyntaxFactory.XmlTextLiteral(
                            tok.LeadingTrivia,
                            newText,
                            newText,
                            tok.TrailingTrivia);

                        nodes[lastMeaningfulIndex] = text.WithTextTokens(SyntaxFactory.TokenList(tokens));
                        return nodes;
                    }

                    // Single-line token: add period before trailing whitespace.
                    string trimmed = s.TrimEnd();
                    string trailing = s.Substring(trimmed.Length);

                    if (trimmed.EndsWith("."))
                    {
                        return nodes;
                    }

                    string newTextSingle = trimmed + "." + trailing;

                    tokens[t] = SyntaxFactory.XmlTextLiteral(
                        tok.LeadingTrivia,
                        newTextSingle,
                        newTextSingle,
                        tok.TrailingTrivia);

                    nodes[lastMeaningfulIndex] = text.WithTextTokens(SyntaxFactory.TokenList(tokens));
                    return nodes;
                }
            }

            // Fallback: add a period as plain text.
            nodes.Add(SyntaxFactory.XmlText("."));
            return nodes;
        }
    }
}