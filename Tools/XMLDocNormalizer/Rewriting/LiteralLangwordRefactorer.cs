using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Rewriting
{
    /// <summary>
    /// Rewriter to replace true/false/null literals in XML documentation comments with <see langword="..."/> tags.
    /// </summary>
    internal sealed class LiteralLangwordRefactorer : CSharpSyntaxRewriter
    {
        /// <summary>
        /// Set of literal keywords to replace.
        /// </summary>
        private static readonly HashSet<string> Literals = new HashSet<string>
    {
        "true",
        "false",
        "null"
    };

        /// <summary>
        /// Visits a syntax trivia and replaces matching literals in XML comments.
        /// </summary>
        /// <param name="trivia">The syntax trivia to visit.</param>
        /// <returns>The possibly modified syntax trivia.</returns>
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

            List<XmlNodeSyntax> newContent = new List<XmlNodeSyntax>();

            foreach (XmlNodeSyntax node in doc.Content)
            {
                newContent.AddRange(ReplaceLiteralsInNode(node));
            }

            DocumentationCommentTriviaSyntax newDoc = doc.WithContent(SyntaxFactory.List(newContent));
            return SyntaxFactory.Trivia(newDoc);
        }

        /// <summary>
        /// Replaces literals in a single XML node recursively.
        /// Returns a list of XmlNodeSyntax to handle multiple replacements in Text nodes.
        /// </summary>
        /// <param name="node">The XML node to process.</param>
        /// <returns>List of possibly modified XML nodes.</returns>
        private IEnumerable<XmlNodeSyntax> ReplaceLiteralsInNode(XmlNodeSyntax node)
        {
            // 1. Text node
            if (node is XmlTextSyntax textNode)
            {
                string fullText = textNode.ToFullString();
                List<XmlNodeSyntax> newNodes = new List<XmlNodeSyntax>();
                int lastIndex = 0;

                Regex regex = new Regex(@"\b(true|false|null)\b", RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(fullText))
                {
                    if (match.Index > lastIndex)
                    {
                        string prefix = fullText.Substring(lastIndex, match.Index - lastIndex);
                        newNodes.Add(SyntaxFactory.XmlText(prefix));
                    }

                    newNodes.Add(CreateSeeLangword(match.Value.ToLowerInvariant()));
                    lastIndex = match.Index + match.Length;
                }

                if (lastIndex < fullText.Length)
                {
                    string suffix = fullText.Substring(lastIndex);
                    newNodes.Add(SyntaxFactory.XmlText(suffix));
                }

                return newNodes.Count > 0 ? newNodes : new List<XmlNodeSyntax> { textNode };
            }

            // 2. <c> or <code> element
            if (node is XmlElementSyntax element &&
                (element.StartTag.Name.LocalName.Text == "c" || element.StartTag.Name.LocalName.Text == "code"))
            {
                string innerText = string.Concat(element.Content.Select(n => n.ToFullString()).ToArray()).Trim();

                if (Literals.Contains(innerText.ToLowerInvariant()))
                {
                    return new List<XmlNodeSyntax> { CreateSeeLangword(innerText.ToLowerInvariant()) };
                }
            }

            // 3. Recursively process child elements
            if (node is XmlElementSyntax el)
            {
                List<XmlNodeSyntax> newChildren = el.Content.SelectMany(ReplaceLiteralsInNode).ToList();
                return new List<XmlNodeSyntax> { el.WithContent(SyntaxFactory.List(newChildren)) };
            }

            return new List<XmlNodeSyntax> { node };
        }

        /// <summary> 
        /// Creates a <see langword="..."/> element for a given literal. 
        /// </summary> 
        /// <param name="literal">The literal to wrap.</param> 
        /// <returns>An XmlEmptyElementSyntax representing the <see> tag.</returns> 
        private static XmlEmptyElementSyntax CreateSeeLangword(string literal)
        {
            var attribute = SyntaxFactory.XmlTextAttribute("langword", literal);
            return SyntaxFactory.XmlEmptyElement("see")
                .WithAttributes(SyntaxFactory.SingletonList<XmlAttributeSyntax>(attribute)
            );
        }
    }
}
