using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents the result of one value-documentation mode run.
    /// </summary>
    internal sealed class ValueDocumentationModeRunDto
    {
        /// <summary>
        /// Gets or sets the value-documentation mode.
        /// </summary>
        public ValueDocumentationMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the total number of findings produced by the mode.
        /// </summary>
        public int FindingCount { get; set; }

        /// <summary>
        /// Gets or sets the number of error findings produced by the mode.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the number of warning findings produced by the mode.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the number of suggestion findings produced by the mode.
        /// </summary>
        public int SuggestionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC800 findings produced by the mode.
        /// </summary>
        public int MissingValueTagCount { get; set; }

        /// <summary>
        /// Gets or sets the number of findings suppressed compared to all-readable-properties mode.
        /// </summary>
        public int SuppressedMissingValueTagCount { get; set; }

        /// <summary>
        /// Gets or sets the findings density per 1000 SLOC.
        /// </summary>
        public double FindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the suggestions density per 1000 SLOC.
        /// </summary>
        public double SuggestionsPerKSloc { get; set; }
    }
}
