namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Distinguishes directly acquired exact bytes from deterministically
    /// reconstructed bytes that subsequently passed the same P5H boundary.
    /// </summary>
    internal enum ExternalSourceMaterialExactness
    {
        /// <summary>The originally acquired byte sequence passed P5H.</summary>
        DirectExact,

        /// <summary>A reconstructed byte sequence passed unchanged P5H.</summary>
        ReconstructedExact
    }
}
