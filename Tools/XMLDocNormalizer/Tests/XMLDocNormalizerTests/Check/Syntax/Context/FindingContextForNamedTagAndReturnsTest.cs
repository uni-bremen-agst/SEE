using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that selected detectors populate finding context metadata.
    /// </summary>
    public sealed class FindingContextForNamedTagAndReturnsTests
    {
        /// <summary>
        /// Ensures that the parameter detector populates owner, subject, accessibility, symbol, and target metadata.
        /// </summary>
        [Fact]
        public void ParamDetector_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Does work.</summary>\n" +
                "public void Resize(int width)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingParamTag.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("Parameter", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Resize", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("width", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that the type parameter detector populates owner, subject, symbol, and target metadata.
        /// </summary>
        [Fact]
        public void TypeParamDetector_PopulatesFindingContext()
        {
            string source =
                "/// <summary>Represents a sample.</summary>\n" +
                "public class Sample<T>\n" +
                "{\n" +
                "    /// <summary>Creates a value.</summary>\n" +
                "    public T Create()\n" +
                "    {\n" +
                "        return default!;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForSource(source);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingTypeParamTag.ID);

            Assert.Equal("Class", finding.Context.OwnerKind);
            Assert.Equal("TypeParameter", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Sample", finding.Context.SymbolName);
            Assert.Equal("Sample", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Equal("T", finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }

        /// <summary>
        /// Ensures that the returns detector populates owner, subject, accessibility, and symbol metadata.
        /// </summary>
        [Fact]
        public void ReturnsDetector_PopulatesFindingContext()
        {
            string memberCode =
                "/// <summary>Calculates a value.</summary>\n" +
                "public int Calculate()\n" +
                "{\n" +
                "    return 1;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.MissingReturns.ID);

            Assert.Equal("Method", finding.Context.OwnerKind);
            Assert.Equal("ReturnValue", finding.Context.SubjectKind);
            Assert.Equal("Public", finding.Context.Accessibility);
            Assert.Equal("Calculate", finding.Context.SymbolName);
            Assert.Equal("C", finding.Context.ContainingType);
            Assert.Equal("GlobalNamespace", finding.Context.ContainingNamespace);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
