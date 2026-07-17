using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Configuration
{
    /// <summary>
    /// Configures which declaration kinds are analyzed by the documentation detectors.
    /// </summary>
    internal sealed class XmlDocOptions
    {
        /// <summary>
        /// Defines the default exception analysis mode used when no explicit mode is configured.
        /// </summary>
        public const ExceptionAnalysisMode DefaultExceptionAnalysisMode =
            ExceptionAnalysisMode.SolutionTransitive;

        /// <summary>
        /// Defines the default value-documentation mode used when no explicit mode is configured.
        /// </summary>
        public const ValueDocumentationMode DefaultValueDocumentationMode =
            ValueDocumentationMode.AllReadableProperties;

        /// <summary>
        /// Gets or sets a value indicating whether enum members are required to have XML documentation.
        /// This affects DOC100, DOC200 and DOC210 for enum members only.
        /// </summary>
        /// <value>
        /// True if enum members are required to have XML documentation; otherwise false.
        /// </value>
        public bool CheckEnumMembers { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether fields are required to have a non-empty summary.
        /// This affects DOC200 and DOC210 for fields only.
        /// </summary>
        /// <value>
        /// True if fields are required to have a non-empty summary; otherwise false.
        /// </value>
        public bool RequireSummaryForFields { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether namespaces are required to have documentation.
        /// This affects DOC100, DOC200 and DOC210 for namespaces only.
        /// </summary>
        /// <value>
        /// True if namespaces are required to have documentation; otherwise false.
        /// </value>
        public bool RequireDocumentationForNamespaces { get; set; } = true;

        /// <summary>
        /// Gets or sets the exception analysis mode.
        /// </summary>
        /// <value>
        /// The exception analysis mode used by semantic exception documentation checks.
        /// </value>
        public ExceptionAnalysisMode ExceptionAnalysisMode { get; set; } =
            DefaultExceptionAnalysisMode;

        /// <summary>
        /// Gets or sets the value-documentation mode.
        /// </summary>
        /// <value>
        /// The value-documentation mode used by missing value-tag checks.
        /// </value>
        public ValueDocumentationMode ValueDocumentationMode { get; set; } =
            DefaultValueDocumentationMode;
    }
}
