using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Represents precomputed semantic information for an extracted exception tag.
    /// </summary>
    internal sealed class ExceptionTagSemanticInfo
    {
        /// <summary>
        /// Gets the extracted exception tag.
        /// </summary>
        /// <value>
        /// The extracted exception tag being analyzed.
        /// </value>
        public required ExtractedXmlDocTag Tag { get; init; }

        /// <summary>
        /// Gets the cref attribute syntax if present and well-formed.
        /// </summary>
        /// <value>
        /// The cref attribute syntax, or null when no well-formed cref attribute is present.
        /// </value>
        public required XmlCrefAttributeSyntax? CrefAttribute { get; init; }

        /// <summary>
        /// Gets the symbol resolved from the cref attribute, if any.
        /// </summary>
        /// <value>
        /// The resolved symbol, or null when the cref target could not be resolved.
        /// </value>
        public required ISymbol? ResolvedSymbol { get; init; }

        /// <summary>
        /// Gets the finding context metadata for findings that are reported on this exception tag.
        /// </summary>
        /// <value>
        /// The finding context metadata associated with this exception tag.
        /// </value>
        public required FindingContext FindingContext { get; init; }

        /// <summary>
        /// Gets the resolved symbol as a named type symbol if applicable.
        /// </summary>
        /// <value>
        /// The resolved named type symbol, or null when the resolved symbol is not a named type.
        /// </value>
        public INamedTypeSymbol? ResolvedTypeSymbol => ResolvedSymbol as INamedTypeSymbol;
    }
}
