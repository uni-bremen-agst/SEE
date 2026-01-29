using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC300: param-tag missing required 'name' attribute.
    /// </summary>
    public sealed class DOC300_ParamMissingNameTests
    {
        /// <summary>
        /// Ensures that a param-tag without a 'name' attribute is detected.
        /// </summary>
        [Fact]
        public void Param_WithoutName_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param>Missing name</param>\n" +
                "int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("param", finding.TagName);
            Assert.Equal("DOC300", finding.Smell.Id);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }
    }
}
