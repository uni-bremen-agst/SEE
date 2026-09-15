using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes embedded-source provenance and retains the exact immutable
    /// uncompressed bytes materialized while validating the Portable PDB.
    /// </summary>
    internal readonly record struct ExternalEmbeddedSourceProvenance
    {
        /// <summary>
        /// Initializes embedded-source provenance.
        /// </summary>
        /// <param name="image">
        /// The exact immutable uncompressed embedded-source bytes.
        /// </param>
        /// <param name="isCompressed">
        /// Whether the embedded content used Deflate compression.
        /// </param>
        /// <param name="isDocumentChecksumValidated">
        /// Whether a known document checksum was successfully validated.
        /// </param>
        private ExternalEmbeddedSourceProvenance(
            ImmutableArray<byte> image,
            bool isCompressed,
            bool isDocumentChecksumValidated)
        {
            Image = image;
            IsCompressed = isCompressed;
            IsDocumentChecksumValidated = isDocumentChecksumValidated;
        }

        /// <summary>
        /// Tries to create validated embedded-source provenance.
        /// </summary>
        /// <param name="image">
        /// The exact immutable uncompressed embedded-source bytes.
        /// </param>
        /// <param name="isCompressed">
        /// Whether the embedded content used Deflate compression.
        /// </param>
        /// <param name="isDocumentChecksumValidated">
        /// Whether a known document checksum was successfully validated.
        /// </param>
        /// <param name="provenance">
        /// The resulting provenance when the size is valid.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="image"/> is an
        /// initialized immutable snapshot; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryCreate(
            ImmutableArray<byte> image,
            bool isCompressed,
            bool isDocumentChecksumValidated,
            out ExternalEmbeddedSourceProvenance provenance)
        {
            if (image.IsDefault)
            {
                provenance = default;
                return false;
            }

            provenance = new ExternalEmbeddedSourceProvenance(
                image,
                isCompressed,
                isDocumentChecksumValidated);
            return true;
        }

        /// <summary>
        /// Gets the exact immutable uncompressed embedded-source bytes.
        /// </summary>
        /// <value>
        /// The bytes materialized and, for a known algorithm, checksum
        /// validated while reading the Portable PDB.
        /// </value>
        public ImmutableArray<byte> Image { get; }

        /// <summary>
        /// Gets whether the embedded source is compressed.
        /// </summary>
        /// <value>
        /// <see langword="true"/> for Deflate-compressed source; otherwise
        /// <see langword="false"/>.
        /// </value>
        public bool IsCompressed { get; }

        /// <summary>
        /// Gets the validated uncompressed source size.
        /// </summary>
        /// <value>The exact retained image length in bytes.</value>
        public int UncompressedSize => Image.Length;

        /// <summary>
        /// Gets whether the uncompressed bytes matched the document checksum.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when a known checksum algorithm was
        /// validated; otherwise <see langword="false"/>.
        /// </value>
        public bool IsDocumentChecksumValidated { get; }
    }
}
