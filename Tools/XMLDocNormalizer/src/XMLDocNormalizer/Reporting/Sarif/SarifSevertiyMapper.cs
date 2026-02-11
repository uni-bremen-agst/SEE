using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Sarif
{
    /// <summary>
    /// Maps tool severities to SARIF levels.
    /// </summary>
    internal static class SarifSeverityMapper
    {
        /// <summary>
        /// Converts a <see cref="Severity"/> value to a SARIF level string.
        /// </summary>
        /// <param name="severity">The tool severity.</param>
        /// <returns>"error", "warning", or "note".</returns>
        public static string ToSarifLevel(Severity severity)
        {
            return severity switch
            {
                Severity.Error => "error",
                Severity.Warning => "warning",
                _ => "note"
            };
        }
    }
}
