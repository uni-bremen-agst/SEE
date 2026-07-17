using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC800 – MissingValueTag.
    /// </summary>
    public sealed class DOC800_MissingValueTagTests
    {
        /// <summary>
        /// Ensures that a readable property with XML documentation but without a value tag triggers DOC800.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(
                finding,
                expectedOwnerKind: "Property",
                expectedSymbolName: "Count",
                expectedTargetName: "Count",
                expectedMessage: "value documentation is missing on property 'Count'.");
        }

        /// <summary>
        /// Ensures that an indexer with XML documentation but without a value tag triggers DOC800.
        /// </summary>
        [Fact]
        public void IndexerWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(
                finding,
                expectedOwnerKind: "Indexer",
                expectedSymbolName: "this[]",
                expectedTargetName: "this[]",
                expectedMessage: "value documentation is missing on indexer 'this[]'.");
        }

        /// <summary>
        /// Ensures that a readable property with a value tag does not trigger DOC800.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithValue_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an indexer with a value tag does not trigger DOC800.
        /// </summary>
        [Fact]
        public void IndexerWithValue_IsNotDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>The indexed value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing value documentation on readable properties.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithInheritdoc_IsNotDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that inheritdoc suppresses missing value documentation on indexers.
        /// </summary>
        [Fact]
        public void IndexerWithInheritdoc_IsNotDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public int this[int i] => i;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that a property without XML documentation is ignored by the value detector.
        /// </summary>
        [Fact]
        public void PropertyWithoutDocumentation_IsIgnored()
        {
            string member =
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an indexer without XML documentation is ignored by the value detector.
        /// </summary>
        [Fact]
        public void IndexerWithoutDocumentation_IsIgnored()
        {
            string member =
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that expression-bodied properties are treated as readable properties.
        /// </summary>
        [Fact]
        public void ExpressionBodiedPropertyWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count => 42;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(
                finding,
                expectedOwnerKind: "Property",
                expectedSymbolName: "Count",
                expectedTargetName: "Count",
                expectedMessage: "value documentation is missing on property 'Count'.");
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC800.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc800()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that empty value documentation is not also reported as missing value documentation.
        /// </summary>
        [Fact]
        public void EmptyValueTag_DoesNotAlsoReportMissingValueTag()
        {
            string member =
                "/// <summary>\n" +
                "/// Gets the count.\n" +
                "/// </summary>\n" +
                "/// <value></value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that duplicate value documentation is not also reported as missing value documentation.
        /// </summary>
        [Fact]
        public void DuplicateValueTags_DoNotAlsoReportMissingValueTag()
        {
            string member =
                "/// <summary>\n" +
                "/// Gets the count.\n" +
                "/// </summary>\n" +
                "/// <value>The count.</value>\n" +
                "/// <value>The count.</value>\n" +
                "public int Count { get; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that value documentation on write-only properties is not also reported as missing value documentation.
        /// </summary>
        [Fact]
        public void WriteOnlyPropertyWithValue_DoesNotAlsoReportMissingValueTag()
        {
            string member =
                "/// <summary>\n" +
                "/// Sets the count.\n" +
                "/// </summary>\n" +
                "/// <value>The count.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that value documentation on non-property members is not also reported as missing value documentation.
        /// </summary>
        [Fact]
        public void MethodWithValue_DoesNotAlsoReportMissingValueTag()
        {
            string member =
                "/// <summary>\n" +
                "/// Does work.\n" +
                "/// </summary>\n" +
                "/// <value>The value.</value>\n" +
                "public void M()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that returns documentation on indexers does not suppress missing value documentation.
        /// </summary>
        [Fact]
        public void IndexerWithReturnsButWithoutValue_ReportsMissingValueTag()
        {
            string member =
                "/// <summary>\n" +
                "/// Gets an item.\n" +
                "/// </summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <returns>The item.</returns>\n" +
                "public int this[int index] => index;\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(member);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.ReturnsOnIndexer.ID,
                XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Asserts a generic missing-value-tag finding with context.
        /// </summary>
        /// <param name="finding">
        /// The finding to assert.
        /// </param>
        /// <param name="expectedOwnerKind">
        /// The expected owner kind.
        /// </param>
        /// <param name="expectedSymbolName">
        /// The expected symbol name.
        /// </param>
        /// <param name="expectedTargetName">
        /// The expected target name.
        /// </param>
        /// <param name="expectedMessage">
        /// The expected formatted message.
        /// </param>
        private static void AssertMissingValueTagFinding(
            Finding finding,
            string expectedOwnerKind,
            string expectedSymbolName,
            string expectedTargetName,
            string expectedMessage)
        {
            Assert.Equal(XmlDocSmells.MissingValueTag.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
            Assert.Equal(expectedOwnerKind, finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal(expectedSymbolName, finding.Context.SymbolName);
            Assert.Equal(expectedTargetName, finding.Context.TargetName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.DoesNotContain("{0}", finding.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("{1}", finding.Message, StringComparison.Ordinal);
        }
    }
}
