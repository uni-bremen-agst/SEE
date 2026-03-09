using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils.Extensions
{
    /// <summary>
    /// Provides extension methods for XML documentation syntax nodes.
    /// </summary>
    internal static class XmlSyntaxExtensions
    {
        /// <summary>
        /// Determines whether the documentation comment contains an <c>inheritdoc</c> tag.
        /// </summary>
        /// <param name="documentationComment">
        /// The documentation comment to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an <c>inheritdoc</c> tag is present; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method recognizes both empty-element and full-element forms, such as
        /// <c>&lt;inheritdoc/&gt;</c> and <c>&lt;inheritdoc&gt;&lt;/inheritdoc&gt;</c>.
        /// Tags with additional attributes, for example <c>cref</c>, are also supported.
        /// </remarks>
        public static bool HasInheritdoc(this DocumentationCommentTriviaSyntax documentationComment)
        {
            ArgumentNullException.ThrowIfNull(documentationComment);

            foreach (XmlNodeSyntax content in documentationComment.Content)
            {
                if (content is XmlEmptyElementSyntax emptyElement &&
                    emptyElement.Name.LocalName.Text == "inheritdoc")
                {
                    return true;
                }

                if (content is XmlElementSyntax element &&
                    element.StartTag.Name.LocalName.Text == "inheritdoc")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
