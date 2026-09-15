using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds an immutable source byte snapshot whose checksum was validated
    /// against the associated Portable PDB document provenance.
    /// </summary>
    /// <remarks>
    /// Checksum validation establishes document identity only. It does not
    /// make the source trusted, executable, repository-backed, or safe.
    /// </remarks>
    internal sealed class ValidatedExternalSourceMaterial
    {
        /// <summary>
        /// Initializes material produced only after exact checksum validation.
        /// </summary>
        /// <param name="document">
        /// The Portable PDB document provenance used for validation.
        /// </param>
        /// <param name="image">The exact immutable validated source bytes.</param>
        /// <param name="origin">How the source bytes were supplied.</param>
        /// <param name="filePath">
        /// The explicit caller path for file material, or
        /// <see langword="null"/> for stream and embedded material.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="document"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal ValidatedExternalSourceMaterial(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<byte> image,
            ExternalSourceMaterialOrigin origin,
            string? filePath)
        {
            ArgumentNullException.ThrowIfNull(document);

            Document = document;
            Image = image;
            Origin = origin;
            FilePath = filePath;
        }

        /// <summary>
        /// Gets the Portable PDB document provenance used for validation.
        /// </summary>
        /// <value>The exact associated document descriptor.</value>
        public ExternalSourceDocumentDescriptor Document { get; }

        /// <summary>
        /// Gets the exact immutable source bytes that passed validation.
        /// </summary>
        /// <value>
        /// The byte-for-byte snapshot, including any byte-order mark and
        /// original newline representation.
        /// </value>
        public ImmutableArray<byte> Image { get; }

        /// <summary>
        /// Gets how the source bytes were supplied.
        /// </summary>
        /// <value>
        /// Source-location provenance only; the origin conveys no trust.
        /// </value>
        public ExternalSourceMaterialOrigin Origin { get; }

        /// <summary>
        /// Gets the optional explicitly supplied source file path.
        /// </summary>
        /// <value>
        /// The caller path for file material, or <see langword="null"/> for
        /// stream and embedded material. It is location provenance only and
        /// does not participate in document identity.
        /// </value>
        public string? FilePath { get; }
    }
}
