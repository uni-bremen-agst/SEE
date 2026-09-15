using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds the ordered Roslyn references reconstructed for every metadata
    /// reference recorded in one compilation provenance descriptor.
    /// </summary>
    /// <remarks>
    /// Completeness is limited to the metadata-reference provenance recorded
    /// by that descriptor. It does not imply a complete runtime dependency set
    /// or that the original compilation can be fully reconstructed.
    /// </remarks>
    internal sealed class ExternalMetadataReferenceSet
    {
        /// <summary>
        /// Initializes an ordered reconstructed metadata-reference set.
        /// </summary>
        /// <param name="references">
        /// The references in original metadata-reference provenance order.
        /// </param>
        internal ExternalMetadataReferenceSet(
            ImmutableArray<PortableExecutableReference> references)
        {
            References = references.IsDefault
                ? ImmutableArray<PortableExecutableReference>.Empty
                : references;
        }

        /// <summary>
        /// Gets the reconstructed references in their original recorded order.
        /// </summary>
        /// <value>The immutable ordered Roslyn reference sequence.</value>
        public ImmutableArray<PortableExecutableReference> References { get; }
    }
}
