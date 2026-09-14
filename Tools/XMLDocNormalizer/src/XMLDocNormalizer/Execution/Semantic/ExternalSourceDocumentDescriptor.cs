using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one source document recorded in a Portable PDB.
    /// </summary>
    internal sealed class ExternalSourceDocumentDescriptor
    {
        /// <summary>
        /// Initializes source-document provenance.
        /// </summary>
        /// <param name="name">The exact document name recorded in metadata.</param>
        /// <param name="hashAlgorithm">The document hash-algorithm GUID.</param>
        /// <param name="hash">The expected source-content hash.</param>
        /// <param name="language">The source-language GUID.</param>
        /// <param name="embeddedSource">
        /// The embedded-source provenance, when present.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public ExternalSourceDocumentDescriptor(
            string name,
            Guid hashAlgorithm,
            ImmutableArray<byte> hash,
            Guid language,
            ExternalEmbeddedSourceProvenance? embeddedSource)
        {
            ArgumentNullException.ThrowIfNull(name);

            Name = name;
            HashAlgorithm = hashAlgorithm;
            Hash = hash.IsDefault ? ImmutableArray<byte>.Empty : hash;
            Language = language;
            EmbeddedSource = embeddedSource;
        }

        /// <summary>
        /// Gets the exact document name recorded in metadata.
        /// </summary>
        /// <value>The unmodified build-provenance document name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the document hash-algorithm GUID.
        /// </summary>
        /// <value>The complete metadata GUID, including unknown values.</value>
        public Guid HashAlgorithm { get; }

        /// <summary>
        /// Gets the expected source-content hash.
        /// </summary>
        /// <value>The immutable document hash bytes.</value>
        public ImmutableArray<byte> Hash { get; }

        /// <summary>
        /// Gets the source-language GUID.
        /// </summary>
        /// <value>The complete language GUID recorded in metadata.</value>
        public Guid Language { get; }

        /// <summary>
        /// Gets the optional embedded-source provenance.
        /// </summary>
        /// <value>
        /// The validated provenance, or <see langword="null"/> when source is
        /// not embedded for this document.
        /// </value>
        public ExternalEmbeddedSourceProvenance? EmbeddedSource { get; }
    }
}
