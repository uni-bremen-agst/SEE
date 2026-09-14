namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes validated provenance for source embedded in a Portable PDB.
    /// </summary>
    internal readonly record struct ExternalEmbeddedSourceProvenance
    {
        /// <summary>
        /// Initializes embedded-source provenance.
        /// </summary>
        /// <param name="isCompressed">
        /// Whether the embedded content uses Deflate compression.
        /// </param>
        /// <param name="uncompressedSize">
        /// The validated uncompressed source size in bytes.
        /// </param>
        /// <param name="isDocumentChecksumValidated">
        /// Whether a known document checksum was successfully validated.
        /// </param>
        private ExternalEmbeddedSourceProvenance(
            bool isCompressed,
            int uncompressedSize,
            bool isDocumentChecksumValidated)
        {
            IsCompressed = isCompressed;
            UncompressedSize = uncompressedSize;
            IsDocumentChecksumValidated = isDocumentChecksumValidated;
        }

        /// <summary>
        /// Tries to create validated embedded-source provenance.
        /// </summary>
        /// <param name="isCompressed">
        /// Whether the embedded content uses Deflate compression.
        /// </param>
        /// <param name="uncompressedSize">
        /// The validated uncompressed source size in bytes.
        /// </param>
        /// <param name="isDocumentChecksumValidated">
        /// Whether a known document checksum was successfully validated.
        /// </param>
        /// <param name="provenance">
        /// The resulting provenance when the size is valid.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="uncompressedSize"/> is
        /// nonnegative; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryCreate(
            bool isCompressed,
            int uncompressedSize,
            bool isDocumentChecksumValidated,
            out ExternalEmbeddedSourceProvenance provenance)
        {
            if (uncompressedSize < 0)
            {
                provenance = default;
                return false;
            }

            provenance = new ExternalEmbeddedSourceProvenance(
                isCompressed,
                uncompressedSize,
                isDocumentChecksumValidated);
            return true;
        }

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
        /// <value>The uncompressed size in bytes.</value>
        public int UncompressedSize { get; }

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
