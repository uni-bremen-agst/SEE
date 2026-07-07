using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the syntax exception detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForSyntaxExceptionDetectorTests
    {
        /// <summary>
        /// Ensures that empty exception description findings contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void EmptyExceptionDescription_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves the value.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\"></exception>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.EmptyExceptionDescription.ID);

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
        /// Ensures that duplicate exception tag findings contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void DuplicateExceptionTag_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves the value.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">First.</exception>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Second.</exception>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.DuplicateExceptionTag.ID);

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
        /// Ensures that rethrow findings contain exception-flow context metadata.
        /// </summary>
        [Fact]
        public void RethrowCannotInferException_PopulatesFindingContext()
        {
            string memberCode =
                "public void Save()\n" +
                "{\n" +
                "    try\n" +
                "    {\n" +
                "    }\n" +
                "    catch\n" +
                "    {\n" +
                "        throw;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.RethrowCannotInferException.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionFlow", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("throw;", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that exception tags on non-executable members contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void ExceptionTagOnNonExecutableMember_PopulatesFindingContext()
        {
            string source =
                "public abstract class C\n" +
                "{\n" +
                "    /// <summary>Saves the value.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown on invalid state.</exception>\n" +
                "    public abstract void Save();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForSource(source);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ExceptionTagOnNonExecutableMember.ID);

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
    }
}
