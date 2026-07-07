using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the basic detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForBasicDetectorTests
    {
        /// <summary>
        /// Ensures that missing documentation findings contain declaration context metadata.
        /// </summary>
        [Fact]
        public void MissingDocumentation_PopulatesFindingContext()
        {
            string memberCode =
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingMethodDocumentation.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("Declaration", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that missing summary findings contain summary-tag context metadata.
        /// </summary>
        [Fact]
        public void MissingSummary_PopulatesFindingContext()
        {
            string memberCode =
                "/// <remarks>Additional information.</remarks>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingSummary.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("SummaryTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that empty remarks findings contain remarks-tag context metadata.
        /// </summary>
        [Fact]
        public void EmptyRemarks_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves the value.</summary>\n" +
                "/// <remarks></remarks>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.EmptyRemarks.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("RemarksTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that top-level tag order findings contain tag-order context metadata.
        /// </summary>
        [Fact]
        public void TopLevelTagOrderMismatch_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Calculates a value.</summary>\n" +
                "/// <returns>The calculated value.</returns>\n" +
                "/// <param name=\"value\">The input value.</param>\n" +
                "public int Calculate(int value)\n" +
                "{\n" +
                "    return value;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.TopLevelTagOrderMismatch.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("TagOrder", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Calculate", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("param", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
