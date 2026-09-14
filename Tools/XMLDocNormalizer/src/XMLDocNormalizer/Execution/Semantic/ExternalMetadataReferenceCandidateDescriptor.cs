using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one PE candidate whose concrete build provenance matches a
    /// serialized compilation metadata reference.
    /// </summary>
    internal sealed class ExternalMetadataReferenceCandidateDescriptor
    {
        /// <summary>
        /// Initializes a validated metadata-reference candidate descriptor.
        /// </summary>
        /// <param name="expectedReference">
        /// The original serialized reference provenance and properties.
        /// </param>
        /// <param name="kind">The actual candidate metadata image kind.</param>
        /// <param name="module">The actual candidate module identity.</param>
        /// <param name="assemblyIdentity">
        /// The actual complete assembly identity, or <see langword="null"/>
        /// for a module candidate.
        /// </param>
        /// <param name="timeDateStamp">The actual COFF timestamp.</param>
        /// <param name="imageSize">The actual PE image size.</param>
        /// <param name="filePath">
        /// The explicitly supplied candidate path, or <see langword="null"/>
        /// for a stream candidate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expectedReference"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal ExternalMetadataReferenceCandidateDescriptor(
            ExternalCompilationMetadataReferenceDescriptor expectedReference,
            MetadataImageKind kind,
            ExternalModuleIdentity module,
            AssemblyIdentity? assemblyIdentity,
            int timeDateStamp,
            int imageSize,
            string? filePath)
        {
            ArgumentNullException.ThrowIfNull(expectedReference);

            ExpectedReference = expectedReference;
            Kind = kind;
            Module = module;
            AssemblyIdentity = assemblyIdentity;
            TimeDateStamp = timeDateStamp;
            ImageSize = imageSize;
            FilePath = filePath;
        }

        /// <summary>
        /// Gets the original serialized reference provenance and properties.
        /// </summary>
        /// <value>The exact expected descriptor supplied for validation.</value>
        public ExternalCompilationMetadataReferenceDescriptor ExpectedReference { get; }

        /// <summary>
        /// Gets the actual candidate metadata image kind.
        /// </summary>
        /// <value>The assembly or module image kind read from metadata.</value>
        public MetadataImageKind Kind { get; }

        /// <summary>
        /// Gets the actual candidate module identity.
        /// </summary>
        /// <value>The metadata module name and MVID.</value>
        public ExternalModuleIdentity Module { get; }

        /// <summary>
        /// Gets the actual complete assembly identity when available.
        /// </summary>
        /// <value>
        /// The assembly identity read from the candidate, or
        /// <see langword="null"/> for a module candidate.
        /// </value>
        public AssemblyIdentity? AssemblyIdentity { get; }

        /// <summary>
        /// Gets the actual candidate COFF timestamp.
        /// </summary>
        /// <value>The uninterpreted signed 32-bit COFF timestamp.</value>
        public int TimeDateStamp { get; }

        /// <summary>
        /// Gets the actual candidate PE image size.
        /// </summary>
        /// <value>The PE optional-header <c>SizeOfImage</c> value.</value>
        public int ImageSize { get; }

        /// <summary>
        /// Gets the explicit candidate path when file-backed.
        /// </summary>
        /// <value>
        /// The candidate path used by the caller, or <see langword="null"/>
        /// for a stream candidate. The path is provenance and not identity.
        /// </value>
        public string? FilePath { get; }
    }
}
