namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Selects whether controlled source acquisition may produce deterministic
    /// line-ending candidates after the original acquired bytes fail P5H.
    /// </summary>
    internal enum ExternalSourceReconstructionPolicy
    {
        /// <summary>
        /// Accepts only directly acquired bytes that pass the Portable PDB
        /// document checksum. This preserves the pre-G3A behavior.
        /// </summary>
        Strict,

        /// <summary>
        /// Permits a bounded set of deterministic line-ending candidates.
        /// Every candidate must still pass unchanged P5H checksum validation.
        /// </summary>
        VerifiedLineEndings
    }
}
