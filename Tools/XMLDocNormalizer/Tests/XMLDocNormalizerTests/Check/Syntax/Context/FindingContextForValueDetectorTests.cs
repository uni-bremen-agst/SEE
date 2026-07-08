using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests finding context metadata produced by the value detector.
    /// </summary>
    public sealed class FindingContextForValueDetectorTests
    {
        /// <summary>
        /// Ensures that a missing value tag on a property carries declaration context.
        /// </summary>
        [Fact]
        public void MissingValueTagOnProperty_PopulatesFindingContext()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(
                findings,
                item => item.Smell.ID == XmlDocSmells.MissingValueTag.ID);

            Assert.Equal("Property", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Count", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("Count", finding.Context.TargetName);
            Assert.False(finding.Context.IsGenerated);
            Assert.False(finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that an empty value tag on an indexer carries declaration context.
        /// </summary>
        [Fact]
        public void EmptyValueTagOnIndexer_PopulatesFindingContext()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value></value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(
                findings,
                item => item.Smell.ID == XmlDocSmells.EmptyValueTag.ID);

            Assert.Equal("Indexer", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("this[]", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("this[]", finding.Context.TargetName);
            Assert.False(finding.Context.IsGenerated);
            Assert.False(finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that a duplicate value tag on an indexer carries declaration context.
        /// </summary>
        [Fact]
        public void DuplicateValueTagOnIndexer_PopulatesFindingContext()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(
                findings,
                item => item.Smell.ID == XmlDocSmells.DuplicateValueTag.ID);

            Assert.Equal("Indexer", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("this[]", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("this[]", finding.Context.TargetName);
            Assert.False(finding.Context.IsGenerated);
            Assert.False(finding.Context.IsTestFile);
        }
    }
}
