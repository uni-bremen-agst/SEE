using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC120: Missing end tag (unclosed XML element).
    /// </summary>
    public sealed class DOC120_MissingEndTagTests
    {
        /// <summary>
        /// Ensures that an unclosed summary-tag is detected.
        /// </summary>
        [Fact]
        public void Malformed_Unclosed_Summary_IsDetected()
        {
            string source =
                "/// <summary>Test\n" +
                "int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("summary", finding.TagName);
            Assert.Equal("DOC120", finding.Smell.Id);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }

        /// <summary>
        /// Ensures that an unclosed returns-tag is detected.
        /// </summary>
        [Fact]
        public void Malformed_Unclosed_Returns_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns>Foo\n" +
                "int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("returns", finding.TagName);
            Assert.Equal("DOC120", finding.Smell.Id);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }

        /// <summary>
        /// Ensures that a mismatched end tag for <returns> is detected as a missing end tag.
        /// </summary>
        [Fact]
        public void Malformed_Mismatched_Returns_EndTag_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns>Foo</return>\n" +
                "int M() { return 0; }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            XMLDocNormalizer.Models.Finding finding = Assert.Single(findings);
            Assert.Equal("returns", finding.TagName);
            Assert.Equal("DOC120", finding.Smell.Id);
        }
    }
}
