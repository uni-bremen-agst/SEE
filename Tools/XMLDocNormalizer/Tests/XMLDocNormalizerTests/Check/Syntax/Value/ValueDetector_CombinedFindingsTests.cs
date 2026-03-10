using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests combined value-related findings on the same documented member.
    /// </summary>
    public sealed class ValueDetector_CombinedFindingsTests
    {
        /// <summary>
        /// Ensures that two empty value tags on a readable property produce
        /// two DOC810 findings and one DOC820 finding.
        /// </summary>
        [Fact]
        public void PropertyWithTwoEmptyValueTags_ProducesEmptyAndDuplicateFindings()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value></value>\n" +
                "/// <value></value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyValueOnProperty.ID,
                XmlDocSmells.EmptyValueOnProperty.ID,
                XmlDocSmells.DuplicateValueOnProperty.ID);
        }

        /// <summary>
        /// Ensures that two empty value tags on an indexer produce
        /// two DOC811 findings and one DOC821 finding.
        /// </summary>
        [Fact]
        public void IndexerWithTwoEmptyValueTags_ProducesEmptyAndDuplicateFindings()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value></value>\n" +
                "/// <value></value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyValueOnIndexer.ID,
                XmlDocSmells.EmptyValueOnIndexer.ID,
                XmlDocSmells.DuplicateValueOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that two non-empty value tags on a readable property produce
        /// only the duplicate finding and no empty-value findings.
        /// </summary>
        [Fact]
        public void PropertyWithTwoNonEmptyValueTags_ProducesOnlyDuplicateFinding()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateValueOnProperty.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that two non-empty value tags on an indexer produce
        /// only the duplicate finding and no empty-value findings.
        /// </summary>
        [Fact]
        public void IndexerWithTwoNonEmptyValueTags_ProducesOnlyDuplicateFinding()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateValueOnIndexer.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that an empty first value tag and a non-empty second value tag
        /// on a readable property produce one DOC810 finding and one DOC820 finding.
        /// </summary>
        [Fact]
        public void PropertyWithEmptyThenNonEmptyValueTag_ProducesEmptyAndDuplicateFindings()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value></value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyValueOnProperty.ID,
                XmlDocSmells.DuplicateValueOnProperty.ID);
        }

        /// <summary>
        /// Ensures that a non-empty first value tag and an empty second value tag
        /// on an indexer produce one DOC811 finding and one DOC821 finding.
        /// </summary>
        [Fact]
        public void IndexerWithNonEmptyThenEmptyValueTag_ProducesEmptyAndDuplicateFindings()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value></value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.EmptyValueOnIndexer.ID,
                XmlDocSmells.DuplicateValueOnIndexer.ID);
        }
    }
}
