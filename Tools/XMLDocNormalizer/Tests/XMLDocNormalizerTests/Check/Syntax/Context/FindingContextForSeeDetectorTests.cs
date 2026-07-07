using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that see detectors populate finding context metadata.
    /// </summary>
    public sealed class FindingContextForSeeDetectorTests
    {
        /// <summary>
        /// Ensures that syntax-based see findings contain see-tag context metadata.
        /// </summary>
        [Fact]
        public void SeeMissingTarget_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Uses <see /> inside the summary.</summary>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.SeeMissingTarget.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("SeeTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that duplicate seealso findings contain seealso-tag context metadata.
        /// </summary>
        [Fact]
        public void DuplicateSeeAlsoTarget_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves the value.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSeeFindingsForMember(memberCode);

            Finding finding = findings.First(item => item.Smell.ID == XmlDocSmells.DuplicateSeeAlsoTarget.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("SeeAlsoTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("cref:string", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that semantic see findings contain see-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidSeeCref_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Uses <see cref=\"MissingType\" /> inside the summary.</summary>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticSeeFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidSeeCref.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("SeeTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("cref:MissingType", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
