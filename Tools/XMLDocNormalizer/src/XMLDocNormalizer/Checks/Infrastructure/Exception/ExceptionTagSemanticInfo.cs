using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Represents precomputed semantic information for an extracted <exception> tag.
    /// </summary>
    internal sealed class ExceptionTagSemanticInfo
    {
        /// <summary>
        /// Gets the extracted exception tag.
        /// </summary>
        public required ExtractedXmlDocTag Tag { get; init; }

        /// <summary>
        /// Gets the cref attribute syntax if present and well-formed.
        /// </summary>
        public required XmlCrefAttributeSyntax? CrefAttribute { get; init; }

        /// <summary>
        /// Gets the symbol resolved from the cref attribute, if any.
        /// </summary>
        public required ISymbol? ResolvedSymbol { get; init; }

        /// <summary>
        /// Gets the resolved symbol as a named type symbol if applicable.
        /// </summary>
        public INamedTypeSymbol? ResolvedTypeSymbol => ResolvedSymbol as INamedTypeSymbol;
    }
}