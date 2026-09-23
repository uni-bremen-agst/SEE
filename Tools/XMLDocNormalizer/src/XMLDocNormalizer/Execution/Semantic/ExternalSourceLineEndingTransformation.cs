namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Identifies the deterministic byte-level line-ending transformation
    /// applied before a reconstructed candidate passed P5H.
    /// </summary>
    internal enum ExternalSourceLineEndingTransformation
    {
        /// <summary>No transformation was applied.</summary>
        None,

        /// <summary>Every bare LF byte was expanded to CRLF.</summary>
        LfToCrlf,

        /// <summary>Every CRLF byte pair was reduced to LF.</summary>
        CrlfToLf
    }
}
