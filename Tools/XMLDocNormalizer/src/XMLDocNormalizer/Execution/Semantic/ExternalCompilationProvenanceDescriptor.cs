namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes compilation metadata from one validated Portable PDB image.
    /// </summary>
    internal sealed class ExternalCompilationProvenanceDescriptor
    {
        /// <summary>
        /// Initializes compilation provenance associated with a validated PDB.
        /// </summary>
        /// <param name="portablePdb">The P4B-validated PDB descriptor.</param>
        /// <param name="compilationOptions">
        /// The serialized compilation options, or <see langword="null"/> when
        /// the corresponding CDI is absent.
        /// </param>
        /// <param name="metadataReferences">
        /// The serialized metadata references, or <see langword="null"/> when
        /// the corresponding CDI is absent.
        /// </param>
        internal ExternalCompilationProvenanceDescriptor(
            ExternalPortablePdbDescriptor portablePdb,
            ExternalCompilationOptionsDescriptor? compilationOptions,
            ExternalCompilationMetadataReferencesDescriptor? metadataReferences)
        {
            PortablePdb = portablePdb;
            CompilationOptions = compilationOptions;
            MetadataReferences = metadataReferences;
        }

        /// <summary>
        /// Gets the P4B descriptor for the exact analyzed PDB image.
        /// </summary>
        /// <value>The validated identity and source provenance.</value>
        public ExternalPortablePdbDescriptor PortablePdb { get; }

        /// <summary>
        /// Gets the compilation-options provenance when present.
        /// </summary>
        /// <value>
        /// The exact serialized options, or <see langword="null"/> when the
        /// module has no Compilation Options CDI.
        /// </value>
        public ExternalCompilationOptionsDescriptor? CompilationOptions { get; }

        /// <summary>
        /// Gets the compilation-metadata-references provenance when present.
        /// </summary>
        /// <value>
        /// The exact serialized references, or <see langword="null"/> when
        /// the module has no Compilation Metadata References CDI.
        /// </value>
        public ExternalCompilationMetadataReferencesDescriptor? MetadataReferences { get; }
    }
}
