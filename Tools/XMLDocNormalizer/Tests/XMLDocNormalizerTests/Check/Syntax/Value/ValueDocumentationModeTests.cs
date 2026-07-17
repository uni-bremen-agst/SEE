using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests configuration modes for missing value documentation.
    /// </summary>
    public sealed class ValueDocumentationModeTests
    {
        /// <summary>
        /// Ensures that none mode suppresses missing value documentation on readable properties.
        /// </summary>
        [Fact]
        public void NoneMode_SuppressesMissingValueOnReadableProperty()
        {
            string member =
                "/// <summary>Gets the count.</summary>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(
                member,
                CreateOptions(ValueDocumentationMode.None));

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that none mode still reports explicitly empty value documentation.
        /// </summary>
        [Fact]
        public void NoneMode_DoesNotSuppressExplicitEmptyValueTag()
        {
            string member =
                "/// <summary>Gets the count.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(
                member,
                CreateOptions(ValueDocumentationMode.None));

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.EmptyValueTag.ID);
        }

        /// <summary>
        /// Ensures that all-readable-properties mode reports missing value documentation on readable properties.
        /// </summary>
        [Fact]
        public void AllReadablePropertiesMode_ReportsMissingValueOnReadableProperty()
        {
            string member =
                "/// <summary>Gets the count.</summary>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(
                member,
                CreateOptions(ValueDocumentationMode.AllReadableProperties));

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that indexers-only mode suppresses missing value documentation on readable properties.
        /// </summary>
        [Fact]
        public void IndexersOnlyMode_SuppressesMissingValueOnReadableProperty()
        {
            string member =
                "/// <summary>Gets the count.</summary>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(
                member,
                CreateOptions(ValueDocumentationMode.IndexersOnly));

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that indexers-only mode reports missing value documentation on indexers.
        /// </summary>
        [Fact]
        public void IndexersOnlyMode_ReportsMissingValueOnIndexer()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "public int this[int index] => index;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(
                member,
                CreateOptions(ValueDocumentationMode.IndexersOnly));

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that DTO-like mode suppresses missing value documentation in DTO namespaces.
        /// </summary>
        [Fact]
        public void ExcludeDtoLikeTypesMode_SuppressesMissingValueInDtoNamespace()
        {
            string source =
                "namespace Sample.Dto\n" +
                "{\n" +
                "    public sealed class RunData\n" +
                "    {\n" +
                "        /// <summary>Gets or sets the count.</summary>\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(
                source,
                CreateOptions(ValueDocumentationMode.ExcludeDtoLikeTypes));

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that DTO-like mode suppresses missing value documentation for DTO type names.
        /// </summary>
        [Fact]
        public void ExcludeDtoLikeTypesMode_SuppressesMissingValueForDtoTypeName()
        {
            string source =
                "namespace Sample\n" +
                "{\n" +
                "    public sealed class RunDto\n" +
                "    {\n" +
                "        /// <summary>Gets or sets the count.</summary>\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(
                source,
                CreateOptions(ValueDocumentationMode.ExcludeDtoLikeTypes));

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that DTO-like mode suppresses missing value documentation for result type names.
        /// </summary>
        [Fact]
        public void ExcludeDtoLikeTypesMode_SuppressesMissingValueForResultTypeName()
        {
            string source =
                "namespace Sample\n" +
                "{\n" +
                "    public sealed class RunResult\n" +
                "    {\n" +
                "        /// <summary>Gets or sets the count.</summary>\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(
                source,
                CreateOptions(ValueDocumentationMode.ExcludeDtoLikeTypes));

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that DTO-like mode still reports missing value documentation on non-DTO properties.
        /// </summary>
        [Fact]
        public void ExcludeDtoLikeTypesMode_ReportsMissingValueForNonDtoTypeName()
        {
            string source =
                "namespace Sample\n" +
                "{\n" +
                "    public sealed class ToolOptions\n" +
                "    {\n" +
                "        /// <summary>Gets or sets the count.</summary>\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(
                source,
                CreateOptions(ValueDocumentationMode.ExcludeDtoLikeTypes));

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Creates XML documentation options for a value-documentation mode.
        /// </summary>
        /// <param name="mode">The value-documentation mode to use.</param>
        /// <returns>Configured XML documentation options.</returns>
        private static XmlDocOptions CreateOptions(ValueDocumentationMode mode)
        {
            return new XmlDocOptions
            {
                ValueDocumentationMode = mode
            };
        }
    }
}
