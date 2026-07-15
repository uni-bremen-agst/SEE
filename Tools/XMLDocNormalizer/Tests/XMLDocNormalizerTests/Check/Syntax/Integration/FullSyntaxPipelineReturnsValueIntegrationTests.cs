using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for returns and value-tag interactions in the full syntax detector pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineReturnsValueIntegrationTests
    {
        /// <summary>
        /// Ensures that missing returns documentation on a non-void method is reported precisely.
        /// </summary>
        [Fact]
        public void NonVoidMethodWithoutReturns_ReportsOnlyMissingReturns()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "public int M()\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingReturns.ID);
        }

        /// <summary>
        /// Ensures that empty and duplicate returns tags are reported precisely.
        /// </summary>
        [Fact]
        public void EmptyAndDuplicateReturns_ReportOnlyReturnsFindings()
        {
            string memberCode =
                "/// <summary>Calculates the result.</summary>\n" +
                "/// <returns></returns>\n" +
                "/// <returns>The duplicate result.</returns>\n" +
                "public int M()\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.DuplicateReturnsTag.ID,
                XmlDocSmells.EmptyReturns.ID);
        }

        /// <summary>
        /// Ensures that returns documentation on a void method is reported by the specific returns rule only.
        /// </summary>
        [Fact]
        public void VoidMethodWithReturns_ReportsOnlyReturnsOnVoidMember()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <returns>Nothing.</returns>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnVoidMember.ID);
        }

        /// <summary>
        /// Ensures that a readable property with summary and value documentation produces no syntax findings.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithValue_DoesNotTriggerAnySyntaxFindings()
        {
            string memberCode =
                "/// <summary>Gets the current count.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that missing value documentation on a readable property is reported precisely.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithoutValue_ReportsOnlyMissingValueTag()
        {
            string memberCode =
                "/// <summary>Gets the current count.</summary>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing value documentation on readable properties
        /// in the full syntax detector pipeline.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithInheritdoc_DoesNotTriggerMissingValueTag()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing value documentation on indexers
        /// in the full syntax detector pipeline.
        /// </summary>
        [Fact]
        public void IndexerWithInheritdoc_DoesNotTriggerMissingValueTag()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "public int this[int index] => index;\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc does not suppress explicitly empty value documentation.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithInheritdocAndEmptyValue_ReportsOnlyEmptyValueTag()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "/// <value></value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.EmptyValueTag.ID);
        }

        /// <summary>
        /// Ensures that empty and duplicate value tags are reported precisely.
        /// </summary>
        [Fact]
        public void EmptyAndDuplicateValueTags_ReportOnlyValueFindings()
        {
            string memberCode =
                "/// <summary>Gets the current count.</summary>\n" +
                "/// <value></value>\n" +
                "/// <value>The duplicate value.</value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.DuplicateValueTag.ID,
                XmlDocSmells.EmptyValueTag.ID);
        }

        /// <summary>
        /// Ensures that a value tag on a write-only property is reported precisely.
        /// </summary>
        [Fact]
        public void WriteOnlyPropertyWithValue_ReportsOnlyValueOnWriteOnlyProperty()
        {
            string memberCode =
                "/// <summary>Sets the current count.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ValueOnWriteOnlyProperty.ID);
        }

        /// <summary>
        /// Ensures that a returns tag on a write-only property is reported by the specific returns rule only.
        /// </summary>
        [Fact]
        public void WriteOnlyPropertyWithReturns_ReportsOnlyReturnsOnWriteOnlyProperty()
        {
            string memberCode =
                "/// <summary>Sets the current count.</summary>\n" +
                "/// <returns>The current count.</returns>\n" +
                "public int Count\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnWriteOnlyProperty.ID);
        }

        /// <summary>
        /// Ensures that a documented indexer with parameter and value documentation produces no syntax findings.
        /// </summary>
        [Fact]
        public void IndexerWithParamAndValue_DoesNotTriggerAnySyntaxFindings()
        {
            string memberCode =
                "/// <summary>Gets a value by index.</summary>\n" +
                "/// <param name=\"index\">The requested index.</param>\n" +
                "/// <value>The value at the requested index.</value>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return index; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that returns documentation on an indexer is reported together with missing value documentation.
        /// </summary>
        [Fact]
        public void IndexerWithReturns_ReportsReturnsOnIndexerAndMissingValue()
        {
            string memberCode =
                "/// <summary>Gets a value by index.</summary>\n" +
                "/// <param name=\"index\">The requested index.</param>\n" +
                "/// <returns>The value at the requested index.</returns>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return index; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingValueTag.ID,
                XmlDocSmells.ReturnsOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that a value tag on an unsupported member kind is reported precisely.
        /// </summary>
        [Fact]
        public void MethodWithValue_ReportsOnlyValueOnInvalidMember()
        {
            string memberCode =
                "/// <summary>Runs the operation.</summary>\n" +
                "/// <value>Invalid value documentation.</value>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ValueOnInvalidMember.ID);
        }
    }
}
