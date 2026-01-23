namespace XMLDocNormalizer.Checks.Configuration
{
    /// <summary>
    /// Configures which declaration kinds are analyzed by the documentation detectors.
    /// </summary>
    internal sealed class XmlDocOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether enum members are required to have XML documentation.
        /// </summary>
        public bool CheckEnumMembers { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether fields are required to have a non-empty summary.
        /// This affects DOC200 and DOC210 for fields only.
        /// </summary>
        public bool RequireSummaryForFields { get; set; } = false;
    }
}
