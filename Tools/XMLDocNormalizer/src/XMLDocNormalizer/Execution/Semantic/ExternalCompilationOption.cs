namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one compiler option serialized in a Portable PDB.
    /// </summary>
    internal sealed class ExternalCompilationOption
    {
        /// <summary>
        /// Initializes serialized compiler-option provenance.
        /// </summary>
        /// <param name="key">The exact serialized option key.</param>
        /// <param name="value">The exact serialized option value.</param>
        internal ExternalCompilationOption(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the exact serialized option key.
        /// </summary>
        /// <value>The unmodified case-sensitive key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets the exact serialized option value.
        /// </summary>
        /// <value>The unmodified value, which may be empty.</value>
        public string Value { get; }
    }
}
