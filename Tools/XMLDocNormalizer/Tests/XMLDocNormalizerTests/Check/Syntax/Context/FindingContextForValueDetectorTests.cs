using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the value detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForValueDetectorTests
    {
        /// <summary>
        /// Ensures that missing value findings on readable properties contain value-tag context metadata.
        /// </summary>
        [Fact]
        public void MissingValueOnProperty_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Gets the count.</summary>\n" +
                "public int Count\n" +
                "{\n" +
                "    get;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingValueOnProperty.ID);

            Assert.Equal("Property", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Count", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("Count", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that empty value findings on indexers contain value-tag context metadata.
        /// </summary>
        [Fact]
        public void EmptyValueOnIndexer_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Gets a value by index.</summary>\n" +
                "/// <value></value>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get\n" +
                "    {\n" +
                "        return index;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.EmptyValueOnIndexer.ID);

            Assert.Equal("Indexer", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("this[]", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("this[]", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that invalid value findings on unsupported members contain value-tag context metadata.
        /// </summary>
        [Fact]
        public void ValueOnInvalidMember_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Does work.</summary>\n" +
                "/// <value>Invalid value text.</value>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ValueOnInvalidMember.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that value findings on write-only properties contain value-tag context metadata.
        /// </summary>
        [Fact]
        public void ValueOnWriteOnlyProperty_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>The assigned value.</value>\n" +
                "public int Count\n" +
                "{\n" +
                "    set\n" +
                "    {\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ValueOnWriteOnlyProperty.ID);

            Assert.Equal("Property", finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Count", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("Count", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
