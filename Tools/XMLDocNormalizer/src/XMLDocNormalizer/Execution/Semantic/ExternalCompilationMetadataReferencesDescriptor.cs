using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes metadata references explicitly serialized in a Portable PDB.
    /// </summary>
    internal sealed class ExternalCompilationMetadataReferencesDescriptor
    {
        /// <summary>
        /// Initializes metadata-reference provenance from validated entries.
        /// </summary>
        /// <param name="references">The entries in original blob order.</param>
        internal ExternalCompilationMetadataReferencesDescriptor(
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> references)
        {
            References = references.IsDefault
                ? ImmutableArray<ExternalCompilationMetadataReferenceDescriptor>.Empty
                : references;
        }

        /// <summary>
        /// Gets the metadata references in original Portable PDB blob order.
        /// </summary>
        /// <value>The exact parsed reference provenance.</value>
        public ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> References { get; }
    }
}
