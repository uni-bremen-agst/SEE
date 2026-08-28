using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents an extracted XML documentation tag and its relevant attribute
    /// value.
    /// </summary>
    /// <remarks>
    /// This type preserves the original XML syntax node together with the
    /// extracted raw attribute value.
    /// </remarks>
    internal sealed class ExtractedXmlDocTag
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="ExtractedXmlDocTag"/> class.
        /// </summary>
        /// <param name="element">
        /// The XML documentation element.
        /// </param>
        /// <param name="rawAttributeValue">
        /// The extracted raw attribute value, if any.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="element"/> is null.
        /// </exception>
        public ExtractedXmlDocTag(
            XmlElementSyntax element,
            string? rawAttributeValue)
        {
            ArgumentNullException.ThrowIfNull(element);

            Element = element;
            RawAttributeValue = rawAttributeValue;
        }

        /// <summary>
        /// Gets the XML documentation element.
        /// </summary>
        /// <value>
        /// The XML documentation element represented by this extracted tag.
        /// </value>
        public XmlElementSyntax Element { get; }

        /// <summary>
        /// Gets the extracted raw attribute value as written in the source.
        /// </summary>
        /// <value>
        /// The extracted raw attribute value, or null when no relevant
        /// attribute value was found.
        /// </value>
        public string? RawAttributeValue { get; }
    }
}
