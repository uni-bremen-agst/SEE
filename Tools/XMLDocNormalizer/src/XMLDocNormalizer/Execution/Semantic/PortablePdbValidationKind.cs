namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes the strength of a successful Portable PDB candidate
    /// validation.
    /// </summary>
    internal enum PortablePdbValidationKind
    {
        /// <summary>
        /// The candidate content ID matches an identity expected by the PE.
        /// </summary>
        Identity,

        /// <summary>
        /// The candidate content ID and at least one PE-provided PDB checksum
        /// match.
        /// </summary>
        IdentityAndChecksum,
    }
}
