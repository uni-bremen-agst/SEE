using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.WellFormed
{
    /// <summary>
    /// Rule tests for DOC110: Unknown or misspelled XML documentation tag.
    /// </summary>
    public sealed class DOC110_UnknownTagTests
    {
        /// <summary>
        /// Ensures that a misspelled summary-tag is detected as unknown.
        /// </summary>
        [Fact]
        public void Malformed_Misspelled_Summary_IsDetected()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("summray", finding.TagName);
            Assert.Equal("DOC110", finding.Smell.Id);
            Assert.Equal(Severity.Warning, finding.Smell.Severity);
        }

        /// <summary>
        /// Ensures that a misspelled <returns> tag (<return>) is detected as an unknown tag.
        /// </summary>
        [Fact]
        public void Malformed_Return_Tag_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <return>Foo</return>\n" +
                "int M() { return 0; }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            XMLDocNormalizer.Models.Finding finding = Assert.Single(findings);
            Assert.Equal("return", finding.TagName);
            Assert.Equal("DOC110", finding.Smell.Id);
        }
    }
}
