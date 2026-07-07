using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that inheritdoc detectors populate finding context metadata.
    /// </summary>
    public sealed class FindingContextForInheritdocDetectorTests
    {
        /// <summary>
        /// Ensures that duplicate inheritdoc findings contain inheritdoc-tag context metadata.
        /// </summary>
        [Fact]
        public void DuplicateInheritdocTag_PopulatesFindingContext()
        {
            string memberCode =
                "/// <inheritdoc />\n" +
                "/// <inheritdoc />\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.DuplicateInheritdocTag.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("InheritdocTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("inheritdoc", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that inheritdoc with own summary findings contain inheritdoc-tag context metadata.
        /// </summary>
        [Fact]
        public void InheritdocWithOwnSummary_PopulatesFindingContext()
        {
            string memberCode =
                "/// <inheritdoc />\n" +
                "/// <summary>Own summary.</summary>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InheritdocWithOwnSummary.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("InheritdocTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("summary", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that invalid inheritdoc cref findings contain inheritdoc-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_PopulatesFindingContext()
        {
            string memberCode =
                "/// <inheritdoc cref=\"DoesNotExist\" />\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidInheritdocCref.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("InheritdocTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("cref:DoesNotExist", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that inheritdoc without source findings contain inheritdoc-tag context metadata.
        /// </summary>
        [Fact]
        public void InheritdocNoSource_PopulatesFindingContext()
        {
            string memberCode =
                "/// <inheritdoc />\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InheritdocNoSource.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("InheritdocTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
