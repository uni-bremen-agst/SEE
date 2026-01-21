namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Defines the severity of a <see cref="Finding"/>.
    /// </summary>
    internal enum Severity
    {
        /// <summary>
        /// Indicates a warning-level finding.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates an error-level finding.
        /// </summary>
        Error
    }
}