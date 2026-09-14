using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes a validated Portable PDB candidate and its source provenance.
    /// </summary>
    internal sealed class ExternalPortablePdbDescriptor
    {
        /// <summary>
        /// Initializes a validated Portable PDB descriptor.
        /// </summary>
        /// <param name="id">The candidate Portable PDB content identifier.</param>
        /// <param name="validationKind">
        /// The successful candidate-validation strength.
        /// </param>
        /// <param name="documents">
        /// The source documents in Portable PDB table order.
        /// </param>
        /// <param name="sourceLink">
        /// The optional validated module Source Link provenance.
        /// </param>
        public ExternalPortablePdbDescriptor(
            BlobContentId id,
            PortablePdbValidationKind validationKind,
            ImmutableArray<ExternalSourceDocumentDescriptor> documents,
            ExternalSourceLinkDescriptor? sourceLink)
        {
            Id = id;
            ValidationKind = validationKind;
            Documents = documents.IsDefault
                ? ImmutableArray<ExternalSourceDocumentDescriptor>.Empty
                : documents;
            SourceLink = sourceLink;
        }

        /// <summary>
        /// Gets the candidate Portable PDB content identifier.
        /// </summary>
        /// <value>The ID read from the Portable PDB metadata header.</value>
        public BlobContentId Id { get; }

        /// <summary>
        /// Gets the successful candidate-validation strength.
        /// </summary>
        /// <value>The identity-only or identity-and-checksum result.</value>
        public PortablePdbValidationKind ValidationKind { get; }

        /// <summary>
        /// Gets the ordered source-document provenance.
        /// </summary>
        /// <value>The documents in Portable PDB table order.</value>
        public ImmutableArray<ExternalSourceDocumentDescriptor> Documents { get; }

        /// <summary>
        /// Gets the optional module Source Link provenance.
        /// </summary>
        /// <value>
        /// The validated Source Link descriptor, or <see langword="null"/>
        /// when the Portable PDB has no module Source Link entry.
        /// </value>
        public ExternalSourceLinkDescriptor? SourceLink { get; }
    }
}
