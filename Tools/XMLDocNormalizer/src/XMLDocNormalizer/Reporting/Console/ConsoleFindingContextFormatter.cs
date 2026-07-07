using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Console
{
    /// <summary>
    /// Formats finding context metadata for human-readable console output.
    /// </summary>
    internal static class ConsoleFindingContextFormatter
    {
        /// <summary>
        /// Formats the context metadata of a finding as a single console line.
        /// </summary>
        /// <param name="finding">The finding whose context should be formatted.</param>
        /// <returns>
        /// A human-readable context line containing owner, subject, accessibility, symbol, type, namespace,
        /// and optional target, project, generated-file, and test-file metadata.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when finding is null.</exception>
        public static string Format(Finding finding)
        {
            ArgumentNullException.ThrowIfNull(finding);

            FindingContext context = finding.Context;

            List<string> parts = new()
            {
                "Owner=" + context.OwnerKind,
                "Subject=" + context.SubjectKind,
                "Accessibility=" + context.Accessibility,
                "Symbol=" + context.SymbolName,
                "Type=" + context.ContainingType,
                "Namespace=" + context.ContainingNamespace
            };

            AddOptionalString(parts, "Target", context.TargetName);
            AddOptionalString(parts, "Project", context.ProjectName);
            AddOptionalBoolean(parts, "Generated", context.IsGenerated);
            AddOptionalBoolean(parts, "TestFile", context.IsTestFile);

            return "Context: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Adds a string value to the formatted parts if the value is present.
        /// </summary>
        /// <param name="parts">The formatted parts to update.</param>
        /// <param name="name">The displayed property name.</param>
        /// <param name="value">The optional property value.</param>
        private static void AddOptionalString(
            List<string> parts,
            string name,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            parts.Add(name + "=" + value);
        }

        /// <summary>
        /// Adds a boolean value to the formatted parts if the value is present.
        /// </summary>
        /// <param name="parts">The formatted parts to update.</param>
        /// <param name="name">The displayed property name.</param>
        /// <param name="value">The optional property value.</param>
        private static void AddOptionalBoolean(
            List<string> parts,
            string name,
            bool? value)
        {
            if (value == null)
            {
                return;
            }

            parts.Add(name + "=" + value.Value);
        }
    }
}
