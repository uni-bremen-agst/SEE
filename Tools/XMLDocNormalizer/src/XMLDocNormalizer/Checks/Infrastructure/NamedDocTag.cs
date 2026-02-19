using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Represents a documentation XML element that references a declared name via a name attribute.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NamedDocTag"/> struct.
    /// </remarks>
    /// <param name="name">The referenced declared name extracted from the name attribute.</param>
    /// <param name="element">The corresponding XML element syntax node.</param>
    internal readonly struct NamedDocTag(string name, XmlElementSyntax element)
    {
        /// <summary>
        /// Gets the referenced declared name extracted from the <c>name</c> attribute.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// Gets the XML element syntax node (e.g. &lt;param&gt; or &lt;typeparam&gt;).
        /// </summary>
        public XmlElementSyntax Element { get; } = element;
    }
}
