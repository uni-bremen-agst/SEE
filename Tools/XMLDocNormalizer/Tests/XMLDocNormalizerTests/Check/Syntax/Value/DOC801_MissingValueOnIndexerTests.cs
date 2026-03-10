using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC801 – MissingValueOnIndexer.
    /// </summary>
    public sealed class DOC801_MissingValueOnIndexerTests
    {
        /// <summary>
        /// Ensures that an indexer with documentation but without a value tag triggers DOC801.
        /// </summary>
        [Fact]
        public void IndexerWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingValueOnIndexer.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that an indexer with a value tag does not trigger DOC801.
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
        /// Ensures that indexers without documentation are ignored here.
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
