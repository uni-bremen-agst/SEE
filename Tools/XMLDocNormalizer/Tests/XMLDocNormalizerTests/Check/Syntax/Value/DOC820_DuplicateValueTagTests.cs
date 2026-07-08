using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC820 – DuplicateValueTag.
    /// </summary>
    public sealed class DOC820_DuplicateValueTagTests
    {
        /// <summary>
        /// Ensures that a second value tag on a readable property triggers DOC820.
        /// </summary>
        [Fact]
        public void SecondValueTagOnProperty_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertDuplicateValueTagFinding(
                finding,
                expectedOwnerKind: "Property",
                expectedSymbolName: "Count",
                expectedTargetName: "Count",
                expectedMessage: "Duplicate value documentation on property 'Count'.");
        }

        /// <summary>
        /// Ensures that a second value tag on an indexer triggers DOC820.
        /// </summary>
        [Fact]
        public void SecondValueTagOnIndexer_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            AssertDuplicateValueTagFinding(
                finding,
                expectedOwnerKind: "Indexer",
                expectedSymbolName: "this[]",
                expectedTargetName: "this[]",
                expectedMessage: "Duplicate value documentation on indexer 'this[]'.");
        }

        /// <summary>
        /// Ensures that every value tag after the first is reported as duplicate on a property.
        /// </summary>
        [Fact]
        public void EveryAdditionalValueTagOnProperty_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "/// <value>Third value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                AssertDuplicateValueTagFinding(
                    finding,
                    expectedOwnerKind: "Property",
                    expectedSymbolName: "Count",
                    expectedTargetName: "Count",
                    expectedMessage: "Duplicate value documentation on property 'Count'.");
            });
        }

        /// <summary>
        /// Ensures that every value tag after the first is reported as duplicate on an indexer.
        /// </summary>
        [Fact]
        public void EveryAdditionalValueTagOnIndexer_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "/// <value>Third value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                AssertDuplicateValueTagFinding(
                    finding,
                    expectedOwnerKind: "Indexer",
                    expectedSymbolName: "this[]",
                    expectedTargetName: "this[]",
                    expectedMessage: "Duplicate value documentation on indexer 'this[]'.");
            });
        }

        /// <summary>
        /// Ensures that a single value tag does not trigger DOC820.
        /// </summary>
        [Fact]
        public void SingleValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.DuplicateValueTag.ID);
        }

        /// <summary>
        /// Ensures that undocumented properties are ignored by the value detector.
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
        /// Ensures that undocumented indexers are ignored by the value detector.
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
        /// Ensures that write-only properties do not trigger DOC820.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc820()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.DuplicateValueTag.ID);
            Assert.Equal(2, findings.Count(finding => finding.Smell.ID == XmlDocSmells.ValueOnWriteOnlyProperty.ID));
        }

        /// <summary>
        /// Asserts a generic duplicate-value-tag finding with context.
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
        private static void AssertDuplicateValueTagFinding(
            Finding finding,
            string expectedOwnerKind,
            string expectedSymbolName,
            string expectedTargetName,
            string expectedMessage)
        {
            Assert.Equal(XmlDocSmells.DuplicateValueTag.ID, finding.Smell.ID);
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
