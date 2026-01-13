using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Rewriting
{
    /// <summary>
    /// Rewrites XML documentation comments in C# code using Roslyn.
    /// </summary>
    internal sealed class XmlDocRewriter : CSharpSyntaxRewriter
    {
        /// <summary>
        /// Visits a single <see cref="SyntaxTrivia"/> and rewrites XML documentation if present.
        /// </summary>
        /// <param name="trivia">The syntax trivia to visit.</param>
        /// <returns>The possibly modified syntax trivia.</returns>
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                return trivia;


            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax doc)
            {
                return trivia;
            }

            SyntaxList<XmlNodeSyntax> newContent = SyntaxFactory.List(
                doc.Content.Select(node =>
                {
                    if (node is XmlElementSyntax element)
                    {
                        string tag = element.StartTag.Name.LocalName.Text;

                        if (tag == "param" || tag == "returns" || tag == "exception")
                        {
                            // Console.WriteLine("Normalize tag: " + tag);
                            // Console.WriteLine(element.ToFullString());
                            // Console.WriteLine("StartTag: " + element.StartTag.Name.LocalName.Text);
                            // Console.WriteLine("EndTag: " + element.EndTag?.Name.LocalName.Text);
                            // Console.WriteLine("Last content node kind: " + element.Content.LastOrDefault()?.Kind().ToString());
                            // Console.WriteLine("Last content node: " + (element.Content.LastOrDefault()?.ToString() ?? "<null>"));
                            return XmlDocTagFormatter.Normalize(element);
                        }
                    }

                    return node;
                })
            );

            DocumentationCommentTriviaSyntax newDoc = doc.WithContent(newContent);

            return SyntaxFactory.Trivia(newDoc);
        }
    }
}