using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds an immutable PE image that was validated against its associated
    /// original metadata-reference provenance.
    /// </summary>
    /// <remarks>
    /// Validation establishes concrete build-provenance matching only. It
    /// does not make the PE trusted or safe to execute and does not represent
    /// a reconstructed Roslyn metadata reference.
    /// </remarks>
    internal sealed class ValidatedExternalMetadataReferenceMaterial
    {
        /// <summary>
        /// Initializes material produced from one successfully validated
        /// immutable candidate image.
        /// </summary>
        /// <param name="candidate">
        /// The P5B descriptor produced from <paramref name="image"/>.
        /// </param>
        /// <param name="image">The exact immutable PE image that was validated.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="candidate"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal ValidatedExternalMetadataReferenceMaterial(
            ExternalMetadataReferenceCandidateDescriptor candidate,
            ImmutableArray<byte> image)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            Candidate = candidate;
            Image = image;
        }

        /// <summary>
        /// Gets the P5B descriptor produced from the retained image.
        /// </summary>
        /// <value>The validated candidate provenance and reference properties.</value>
        public ExternalMetadataReferenceCandidateDescriptor Candidate { get; }

        /// <summary>
        /// Gets the exact immutable PE image validated for the candidate.
        /// </summary>
        /// <value>
        /// The complete PE bytes retained independently of later filesystem
        /// changes.
        /// </value>
        public ImmutableArray<byte> Image { get; }
    }
}
