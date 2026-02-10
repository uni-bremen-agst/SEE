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