using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Context
{
    /// <summary>
    /// Tests that namespace documentation aggregation populates finding context metadata.
    /// </summary>
    public sealed class FindingContextForNamespaceDocumentationTests
    {
        /// <summary>
        /// Ensures that missing central namespace documentation findings contain namespace documentation context metadata.
        /// </summary>
        [Fact]
        public void MissingCentralNamespaceDocumentation_PopulatesFindingContext()
        {
            XmlDocOptions options = new XmlDocOptions();
            options.RequireDocumentationForNamespaces = true;

            (string FileName, string Source)[] sources = new[]
            {
                (
                    "SugiyamaLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Represents a layout.</summary>\n" +
                    "    public sealed class SugiyamaLayout\n" +
                    "    {\n" +
                    "    }\n" +
                    "}\n"
                )
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSources(sources, options);

            Finding finding = findings.Single(
                item => item.Smell.ID == XmlDocSmells.MissingCentralNamespaceDocumentation.ID);

            Assert.Equal("Namespace", finding.Context.OwnerKind);
            Assert.Equal("NamespaceDocumentation", finding.Context.SubjectKind);
            Assert.Equal("NotApplicable", finding.Context.Accessibility);
            Assert.Equal("SEE.Layout.EdgeLayouts", finding.Context.SymbolName);
            Assert.Equal("None", finding.Context.ContainingType);
            Assert.Equal("SEE.Layout.EdgeLayouts", finding.Context.ContainingNamespace);
            Assert.Equal("SEE.Layout.EdgeLayouts", finding.Context.TargetName);
            Assert.Null(finding.Context.ProjectName);
            Assert.Equal(false, finding.Context.IsGenerated);
            Assert.Equal(false, finding.Context.IsTestFile);
        }
    }
}
