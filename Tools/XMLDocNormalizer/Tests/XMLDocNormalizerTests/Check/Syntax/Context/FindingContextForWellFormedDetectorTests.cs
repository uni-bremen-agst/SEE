using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that the well-formed detector populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForWellFormedDetectorTests
    {
        /// <summary>
        /// Ensures that non-empty paramref findings contain parameter-reference context metadata.
        /// </summary>
        [Fact]
        public void ParamRefNotEmpty_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Uses <paramref name=\"value\">the value</paramref>.</summary>\n" +
                "public void Save(int value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ParamRefNotEmpty.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ParamRefTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("value", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that non-empty typeparamref findings contain type-parameter-reference context metadata.
        /// </summary>
        [Fact]
        public void TypeParamRefNotEmpty_PopulatesFindingContext()
        {
            string source =
                "public class C<T>\n" +
                "{\n" +
                "    /// <summary>Uses <typeparamref name=\"T\">the type</typeparamref>.</summary>\n" +
                "    public void Save()\n" +
                "    {\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForSource(source);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.TypeParamRefNotEmpty.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("TypeParamRefTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("T", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that param tags without name attributes contain parameter-tag context metadata.
        /// </summary>
        [Fact]
        public void ParamMissingName_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves a value.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public void Save(int value)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ParamMissingName.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ParameterTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that exception tags without cref attributes contain exception-tag context metadata.
        /// </summary>
        [Fact]
        public void ExceptionMissingCref_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves a value.</summary>\n" +
                "/// <exception>Missing cref.</exception>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.ExceptionMissingCref.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ExceptionTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that unknown XML documentation tags contain XML-tag context metadata.
        /// </summary>
        [Fact]
        public void UnknownTag_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Saves a value.</summary>\n" +
                "/// <unknown>Unsupported tag.</unknown>\n" +
                "public void Save()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.UnknownTag.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("XmlTag", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Save", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("unknown", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
