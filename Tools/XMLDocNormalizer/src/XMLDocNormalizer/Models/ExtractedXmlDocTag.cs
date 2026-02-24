using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents an extracted XML documentation element along with an extracted attribute value.
    /// </summary>
    /// <remarks>
    /// This type is intended as a lightweight transport structure for detectors that work on
    /// documentation tags containing a relevant attribute value (e.g. name, cref).
    /// </remarks>
    internal readonly struct ExtractedXmlDocTag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractedXmlDocTag"/> struct.
        /// </summary>
        /// <param name="element">The XML documentation element.</param>
        /// <param name="rawAttributeValue">The extracted raw attribute value, if any.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="element"/> is <c>null</c>.
        /// </exception>
        public ExtractedXmlDocTag(XmlElementSyntax element, string? rawAttributeValue)
        {
            ArgumentNullException.ThrowIfNull(element);

            Element = element;
            RawAttributeValue = rawAttributeValue;
        }

        /// <summary>
        /// Gets the XML documentation element.
        /// </summary>
        public XmlElementSyntax Element { get; }

        /// <summary>
        /// Gets the extracted raw attribute value as written in the source.
        /// </summary>
        public string? RawAttributeValue { get; }
    }
}