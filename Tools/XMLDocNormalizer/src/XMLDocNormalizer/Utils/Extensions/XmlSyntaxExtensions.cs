using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils.Extensions
{
    /// <summary>
    /// Provides extension methods for XML documentation syntax nodes.
    /// </summary>
    internal static class XmlSyntaxExtensions
    {
        /// <summary>
        /// Determines whether the documentation comment contains an inheritdoc tag.
        /// </summary>
        /// <param name="documentationComment">
        /// The documentation comment to inspect.
        /// </param>
        /// <returns>
        /// True if an inheritdoc tag is present; otherwise false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="documentationComment"/> is null.
        /// </exception>
        /// <remarks>
        /// This method recognizes both self-closing inheritdoc tags and full inheritdoc elements.
        /// Inheritdoc tags with additional attributes, such as a cref attribute, are also supported.
        /// </remarks>
        public static bool HasInheritdoc(
            this DocumentationCommentTriviaSyntax documentationComment)
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
