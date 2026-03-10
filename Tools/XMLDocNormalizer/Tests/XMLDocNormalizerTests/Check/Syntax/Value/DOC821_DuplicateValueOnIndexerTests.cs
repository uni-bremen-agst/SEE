using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC821 – DuplicateValueOnIndexer.
    /// </summary>
    public sealed class DOC821_DuplicateValueOnIndexerTests
    {
        /// <summary>
        /// Ensures that a second value tag on an indexer triggers DOC821.
        /// </summary>
        [Fact]
        public void SecondValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateValueOnIndexer.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that every value tag after the first is reported as duplicate.
        /// </summary>
        [Fact]
        public void EveryAdditionalValueTag_IsDetected()
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
                Assert.Equal(XmlDocSmells.DuplicateValueOnIndexer.ID, finding.Smell.ID);
                Assert.Equal("value", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that a single value tag does not trigger DOC821.
        /// </summary>
        [Fact]
        public void SingleValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>The indexed value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.DuplicateValueOnIndexer.ID);
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
