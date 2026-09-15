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
        /// <param name="debugDirectory">
        /// The P4A debug provenance that validated the PDB candidate.
        /// </param>
        /// <param name="portablePdb">The P4B-validated PDB descriptor.</param>
        /// <param name="compilationOptions">
        /// The serialized compilation options, or <see langword="null"/> when
        /// the corresponding CDI is absent.
        /// </param>
        /// <param name="metadataReferences">
        /// The serialized metadata references, or <see langword="null"/> when
        /// the corresponding CDI is absent.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="debugDirectory"/> or
        /// <paramref name="portablePdb"/> is <see langword="null"/>.
        /// </exception>
        internal ExternalCompilationProvenanceDescriptor(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            ExternalPortablePdbDescriptor portablePdb,
            ExternalCompilationOptionsDescriptor? compilationOptions,
            ExternalCompilationMetadataReferencesDescriptor? metadataReferences)
        {
            ArgumentNullException.ThrowIfNull(debugDirectory);
            ArgumentNullException.ThrowIfNull(portablePdb);

            DebugDirectory = debugDirectory;
            PortablePdb = portablePdb;
            CompilationOptions = compilationOptions;
            MetadataReferences = metadataReferences;
        }

        /// <summary>
        /// Gets the P4A target-module and debug provenance used to validate the
        /// exact Portable PDB candidate.
        /// </summary>
        /// <value>The validated PE debug-directory provenance.</value>
        public ExternalPeDebugDirectoryDescriptor DebugDirectory { get; }

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
