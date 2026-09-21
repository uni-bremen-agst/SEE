namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Selects one Portable PDB document ordinal and its explicit acquisition
    /// mode for a reconstructed source-tree ordinal.
    /// </summary>
    internal sealed record ExternalSourceReconstructionInput
    {
        /// <summary>
        /// Initializes one immutable source reconstruction input.
        /// </summary>
        /// <param name="documentOrdinal">
        /// The zero-based ordinal in the validated Portable PDB document table.
        /// </param>
        /// <param name="candidatePath">
        /// The exact source candidate path, or <see langword="null"/> to use
        /// validated embedded source from the selected document.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="documentOrdinal"/> is negative.
        /// </exception>
        public ExternalSourceReconstructionInput(
            int documentOrdinal,
            string? candidatePath)
            : this(documentOrdinal, candidatePath, allowAcquisition: false)
        {
        }

        /// <summary>
        /// Creates an input that uses Embedded Source first and otherwise
        /// permits context-configured P7B acquisition.
        /// </summary>
        /// <param name="documentOrdinal">
        /// The zero-based ordinal in the validated Portable PDB document table.
        /// </param>
        /// <returns>An acquisition-enabled source reconstruction input.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="documentOrdinal"/> is negative.
        /// </exception>
        public static ExternalSourceReconstructionInput CreateAcquirable(
            int documentOrdinal)
        {
            return new ExternalSourceReconstructionInput(
                documentOrdinal,
                candidatePath: null,
                allowAcquisition: true);
        }

        /// <summary>
        /// Initializes one explicit, embedded-only, or acquisition-enabled input.
        /// </summary>
        /// <param name="documentOrdinal">The Portable PDB document ordinal.</param>
        /// <param name="candidatePath">The authoritative explicit path.</param>
        /// <param name="allowAcquisition">
        /// Whether configured acquisition may follow an unavailable Embedded Source.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="documentOrdinal"/> is negative.
        /// </exception>
        private ExternalSourceReconstructionInput(
            int documentOrdinal,
            string? candidatePath,
            bool allowAcquisition)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(documentOrdinal);

            DocumentOrdinal = documentOrdinal;
            CandidatePath = candidatePath;
            AllowAcquisition = allowAcquisition;
        }

        /// <summary>
        /// Gets the Portable PDB document-table ordinal.
        /// </summary>
        /// <value>The zero-based document ordinal.</value>
        public int DocumentOrdinal { get; }

        /// <summary>
        /// Gets the exact explicit source candidate path.
        /// </summary>
        /// <value>
        /// The file path, or <see langword="null"/> when embedded source must
        /// be used. A path is provenance and is never inferred from a document
        /// name.
        /// </value>
        public string? CandidatePath { get; }

        /// <summary>
        /// Gets whether context-configured acquisition may run after the
        /// Embedded Source fast path is unavailable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> only for an explicit P7B acquisition opt-in.
        /// </value>
        public bool AllowAcquisition { get; }
    }
}
