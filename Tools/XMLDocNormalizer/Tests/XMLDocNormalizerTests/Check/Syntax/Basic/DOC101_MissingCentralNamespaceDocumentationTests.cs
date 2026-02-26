using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Tests for DOC101 – MissingCentralNamespaceDocumentation.
    /// </summary>
    public sealed class DOC101_MissingCentralNamespaceDocumentationTests
    {
        /// <summary>
        /// Ensures that two files in the same directory declaring the same namespace without namespace XML docs
        /// produce exactly one DOC101 finding (aggregation).
        /// </summary>
        [Fact]
        public void TwoFilesSameNamespace_NoCentralDoc_EmitsSingleDoc101()
        {
            XmlDocOptions options = new XmlDocOptions();
            options.RequireDocumentationForNamespaces = true;

            (string FileName, string Source)[] sources = new[]
            {
                ("SugiyamaLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Test.</summary>\n" +
                    "    public sealed class SugiyamaLayout { }\n" +
                    "}\n"),
                ("CircularLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Test.</summary>\n" +
                    "    public sealed class CircularLayout { }\n" +
                    "}\n")
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSources(sources, options);

            List<Finding> doc101 = findings.Where(f => f.Smell.Id == "DOC101").ToList();
            Assert.Single(doc101);

            // Ensure we don't get namespace DOC100 noise per file.
            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC100" && f.TagName == "documentation");
        }

        /// <summary>
        /// Ensures that a preferred central namespace documentation file (EdgeLayouts.cs) suppresses DOC101.
        /// </summary>
        [Fact]
        public void CentralNamespaceFile_SuppressesDoc101()
        {
            XmlDocOptions options = new XmlDocOptions();
            options.RequireDocumentationForNamespaces = true;

            (string FileName, string Source)[] sources = new[]
            {
                ("EdgeLayouts.cs",
                    "/// <summary>Contains edge layout implementations.</summary>\n" +
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "}\n"),
                ("SugiyamaLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Test.</summary>\n" +
                    "    public sealed class SugiyamaLayout { }\n" +
                    "}\n")
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSources(sources, options);

            Assert.DoesNotContain(findings, f => f.Smell.Id == "DOC101");
        }

        /// <summary>
        /// Ensures that a documented namespace in a non-preferred file does not count as central documentation
        /// and therefore DOC101 is still emitted.
        /// </summary>
        [Fact]
        public void DocumentedNamespaceInNonPreferredFile_StillEmitsDoc101()
        {
            XmlDocOptions options = new XmlDocOptions();
            options.RequireDocumentationForNamespaces = true;

            (string FileName, string Source)[] sources = new[]
            {
                ("Other.cs",
                    "/// <summary>Wrong place on purpose.</summary>\n" +
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "}\n"),
                ("CircularLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Test.</summary>\n" +
                    "    public sealed class CircularLayout { }\n" +
                    "}\n")
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSources(sources, options);

            Assert.Single(findings.Where(f => f.Smell.Id == "DOC101"));
        }

        /// <summary>
        /// Ensures that DOC101 message mentions the suggested file names and that the snippet contains a stub.
        /// </summary>
        [Fact]
        public void Doc101_MessageMentionsSuggestedFileNames_AndProvidesStubSnippet()
        {
            XmlDocOptions options = new XmlDocOptions();
            options.RequireDocumentationForNamespaces = true;

            (string FileName, string Source)[] sources = new[]
            {
                ("SugiyamaLayout.cs",
                    "namespace SEE.Layout.EdgeLayouts\n" +
                    "{\n" +
                    "    /// <summary>Test.</summary>\n" +
                    "    public sealed class SugiyamaLayout { }\n" +
                    "}\n")
            };

            List<Finding> findings = CheckAssert.FindBasicFindingsForSources(sources, options);

            Finding finding = Assert.Single(findings.Where(f => f.Smell.Id == "DOC101"));

            Assert.Contains("EdgeLayouts.cs", finding.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("EdgeLayout.cs", finding.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("namespace SEE.Layout.EdgeLayouts", finding.Snippet, StringComparison.Ordinal);
        }
    }
}