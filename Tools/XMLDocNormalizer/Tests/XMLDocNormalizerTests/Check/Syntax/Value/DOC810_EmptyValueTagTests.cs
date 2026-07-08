using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC810 – EmptyValueTag.
    /// </summary>
    public sealed class DOC810_EmptyValueTagTests
    {
        /// <summary>
        /// Ensures that an empty value tag on a readable property triggers DOC810.
        /// </summary>
        [Fact]
        public void EmptyValueTagOnProperty_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertEmptyValueTagFinding(
                finding,
                expectedOwnerKind: "Property",
                expectedSymbolName: "Count",
                expectedTargetName: "Count",
                expectedMessage: "value documentation on property 'Count' is empty.");
        }

        /// <summary>
        /// Ensures that a whitespace-only value tag on a readable property triggers DOC810.
        /// </summary>
        [Fact]
        public void WhitespaceOnlyValueTagOnProperty_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>   </value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertEmptyValueTagFinding(
                finding,
                expectedOwnerKind: "Property",
                expectedSymbolName: "Count",
                expectedTargetName: "Count",
                expectedMessage: "value documentation on property 'Count' is empty.");
        }

        /// <summary>
        /// Ensures that an empty value tag on an indexer triggers DOC810.
        /// </summary>
        [Fact]
        public void EmptyValueTagOnIndexer_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value></value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertEmptyValueTagFinding(
                finding,
                expectedOwnerKind: "Indexer",
                expectedSymbolName: "this[]",
                expectedTargetName: "this[]",
                expectedMessage: "value documentation on indexer 'this[]' is empty.");
        }

        /// <summary>
        /// Ensures that a whitespace-only value tag on an indexer triggers DOC810.
        /// </summary>
        [Fact]
        public void WhitespaceOnlyValueTagOnIndexer_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>   </value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertEmptyValueTagFinding(
                finding,
                expectedOwnerKind: "Indexer",
                expectedSymbolName: "this[]",
                expectedTargetName: "this[]",
                expectedMessage: "value documentation on indexer 'this[]' is empty.");
        }

        /// <summary>
        /// Ensures that a non-empty value tag on a readable property does not trigger DOC810.
        /// </summary>
        [Fact]
        public void NonEmptyValueTagOnProperty_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a non-empty value tag on an indexer does not trigger DOC810.
        /// </summary>
        [Fact]
        public void NonEmptyValueTagOnIndexer_IsNotDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>The indexed value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a missing value tag is not reported as DOC810.
        /// </summary>
        [Fact]
        public void MissingValueTag_DoesNotTriggerDoc810()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.EmptyValueTag.ID);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.MissingValueTag.ID);
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC810.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc810()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.EmptyValueTag.ID);
            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnWriteOnlyProperty.ID);
        }

        /// <summary>
        /// Asserts a generic empty-value-tag finding with context.
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
        private static void AssertEmptyValueTagFinding(
            Finding finding,
            string expectedOwnerKind,
            string expectedSymbolName,
            string expectedTargetName,
            string expectedMessage)
        {
            Assert.Equal(XmlDocSmells.EmptyValueTag.ID, finding.Smell.ID);
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
