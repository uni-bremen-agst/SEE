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
        /// <param name="sourceIdentity">
        /// The original local path or credential-free Source Link URI.
        /// </param>
        /// <param name="exactness">
        /// Whether original or deterministically reconstructed bytes passed P5H.
        /// </param>
        /// <param name="transformation">
        /// The reconstruction transformation, or <c>None</c> for direct bytes.
        /// </param>
        /// <remarks>
        /// Callers are internal factories that have already validated the
        /// document and exactness/transformation pairing.
        /// </remarks>
        internal ValidatedExternalSourceMaterial(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<byte> image,
            ExternalSourceMaterialOrigin origin,
            string? filePath,
            string? sourceIdentity,
            ExternalSourceMaterialExactness exactness,
            ExternalSourceLineEndingTransformation transformation)
        {
            Document = document;
            Image = image;
            Origin = origin;
            FilePath = filePath;
            SourceIdentity = sourceIdentity;
            Exactness = exactness;
            Transformation = transformation;
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

        /// <summary>
        /// Gets the original controlled candidate identity before reconstruction.
        /// </summary>
        /// <value>
        /// The explicit or mapped local path, the credential-free Source Link
        /// URI, or <see langword="null"/> for stream and embedded material.
        /// </value>
        public string? SourceIdentity { get; }

        /// <summary>
        /// Gets whether direct or reconstructed bytes passed P5H.
        /// </summary>
        /// <value>The cryptographically validated source exactness category.</value>
        public ExternalSourceMaterialExactness Exactness { get; }

        /// <summary>
        /// Gets the deterministic transformation applied before P5H validation.
        /// </summary>
        /// <value><c>None</c> for directly exact material.</value>
        public ExternalSourceLineEndingTransformation Transformation { get; }
    }
}
