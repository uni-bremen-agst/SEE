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
        {
            ArgumentOutOfRangeException.ThrowIfNegative(documentOrdinal);

            DocumentOrdinal = documentOrdinal;
            CandidatePath = candidatePath;
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
    }
}
