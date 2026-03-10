using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC811 – EmptyValueOnIndexer.
    /// </summary>
    public sealed class DOC811_EmptyValueOnIndexerTests
    {
        /// <summary>
        /// Ensures that an empty value tag on an indexer triggers DOC811.
        /// </summary>
        [Fact]
        public void EmptyValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value></value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.EmptyValueOnIndexer.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a whitespace-only value tag on an indexer triggers DOC811.
        /// </summary>
        [Fact]
        public void WhitespaceOnlyValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>   </value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.EmptyValueOnIndexer.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a non-empty value tag on an indexer does not trigger DOC811.
        /// </summary>
        [Fact]
        public void NonEmptyValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>The indexed value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a missing value tag is not reported as DOC811.
        /// </summary>
        [Fact]
        public void MissingValueTag_DoesNotTriggerDoc811()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.EmptyValueOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that undocumented indexers are ignored here.
        /// </summary>
        [Fact]
        public void IndexerWithoutDocumentation_IsIgnored()
        {
            string member =
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}