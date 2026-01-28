using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.WellFormed
{
    /// <summary>
    /// Rule tests for DOC400: typeparam-tag missing required 'name' attribute.
    /// </summary>
    public sealed class DOC400_TypeParamMissingNameTests
    {
        /// <summary>
        /// Ensures that a typeparam-tag without a 'name' attribute is detected.
        /// </summary>
        [Fact]
        public void TypeParam_WithoutName_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <typeparam>Missing name</typeparam>\n" +
                "int M<T>(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal("typeparam", finding.TagName);
            Assert.Equal("DOC400", finding.Smell.Id);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }
    }
}
