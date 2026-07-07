using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the member tag detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForMemberTagDetectorTests
    {
        /// <summary>
        /// Ensures that invalid tags on fields contain invalid-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidTagOnField_PopulatesFindingContext()
        {
            string memberCode =
                "/// <returns>Invalid return documentation.</returns>\n" +
                "public int Count;\n";

            List<Finding> findings = CheckAssert.FindMemberTagFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);

            Assert.Equal("Field", finding.Context.OwnerKind);
            Assert.Equal("InvalidTagUsage", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Count", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("returns", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that invalid tags on properties contain invalid-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidTagOnProperty_PopulatesFindingContext()
        {
            string memberCode =
                "/// <param name=\"value\">Invalid parameter documentation.</param>\n" +
                "public int Count\n" +
                "{\n" +
                "    get;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindMemberTagFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);

            Assert.Equal("Property", finding.Context.OwnerKind);
            Assert.Equal("InvalidTagUsage", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Count", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("param", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that invalid tags on enum members contain invalid-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidTagOnEnumMember_PopulatesFindingContext()
        {
            string source =
                "public enum Status\n" +
                "{\n" +
                "    /// <returns>Invalid return documentation.</returns>\n" +
                "    Active\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindMemberTagFindingsForSource(source);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);

            Assert.Equal("EnumMember", finding.Context.OwnerKind);
            Assert.Equal("InvalidTagUsage", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Active", finding.Context.SymbolName);
            Assert.Equal("Status", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("returns", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
