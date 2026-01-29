using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC600: exception-tag missing required 'cref' attribute.
    /// </summary>
    public sealed class DOC600_ExceptionMissingCrefTests
    {
        /// <summary>
        /// Ensures that an exception-tag without a 'cref' attribute is detected.
        /// </summary>
        [Fact]
        public void Exception_WithoutCref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <exception>Missing cref</exception>\n" +
                "void M() {}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("exception", finding.TagName);
            Assert.Equal("DOC600", finding.Smell.Id);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }
    }
}
