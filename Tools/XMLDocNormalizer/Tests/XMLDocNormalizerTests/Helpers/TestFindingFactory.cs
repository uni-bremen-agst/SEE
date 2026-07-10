using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helper methods to create deterministic Finding instances for tests.
    /// </summary>
    internal static class TestFindingFactory
    {
        /// <summary>
        /// Creates a single Finding with predictable values.
        /// </summary>
        /// <param name="smellId">The smell id.</param>
        /// <param name="severity">The severity for the smell.</param>
        /// <param name="filePath">The file path used by the finding.</param>
        /// <param name="tagName">The XML tag name used by the finding.</param>
        /// <param name="line">1-based line.</param>
        /// <param name="column">1-based column.</param>
        /// <returns>A deterministic finding.</returns>
        public static Finding Create(
            string smellId,
            Severity severity = Severity.Warning,
            string filePath = "File.cs",
            string tagName = "summary",
            int line = 10,
            int column = 5)
        {
            XmlDocSmell smell = new(
                id: smellId,
                messageTemplate: "Message template for tests.",
                severity: severity,
                ruleTitle: "Test rule",
                ruleDescription: "Reports deterministic test findings.");

            return new Finding(
                smell: smell,
                filePath: filePath,
                tagName: tagName,
                line: line,
                column: column,
                snippet: "<summary>Snippet</summary>");
        }
    }
}
