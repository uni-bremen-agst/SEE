using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the semantic exception detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForSemanticExceptionDetectorTests
    {
        /// <summary>
        /// Ensures that invalid exception cref findings contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void InvalidExceptionCref_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Does work.</summary>\n" +
                "/// <exception cref=\"NotExistingException\">Invalid.</exception>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    memberCode,
                    ExceptionAnalysisMode.Direct);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidExceptionCref.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("cref:NotExistingException", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that documented exceptions without direct throws contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void ExceptionTagWithoutDirectThrow_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Does work.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Documented.</exception>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    memberCode,
                    ExceptionAnalysisMode.Direct);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("cref:System.InvalidOperationException", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that missing direct exception documentation findings contain exception-flow context metadata.
        /// </summary>
        [Fact]
        public void MissingExceptionTag_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Does work.</summary>\n" +
                "public void Save()\n" +
                "{\n" +
                "    throw new System.InvalidOperationException();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    memberCode,
                    ExceptionAnalysisMode.Direct);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionFlow", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("System.InvalidOperationException", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that missing transitive exception documentation findings contain exception-flow context metadata.
        /// </summary>
        [Fact]
        public void MissingTransitiveExceptionDocumentation_PopulatesFindingContext()
        {
            string source =
                "public class C\n" +
                "{\n" +
                "    /// <summary>Does work.</summary>\n" +
                "    public void Save()\n" +
                "    {\n" +
                "        Helper();\n" +
                "    }\n" +
                "\n" +
                "    private void Helper()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionFlow", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("System.InvalidOperationException", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
