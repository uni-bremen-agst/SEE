using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.WellFormed
{
    /// <summary>
    /// Rule tests for DOC430: typeparamref should be an empty element.
    /// </summary>
    public sealed class DOC430_TypeParamRefNotEmptyTests
    {
        /// <summary>
        /// Ensures that a non-empty typeparamref-tag is detected.
        /// </summary>
        [Fact]
        public void Malformed_Typeref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <remarks><typeparamref name=\"T\"> is invalid</remarks>\n" +
                "int M<T>() { return 0; }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindMalformedForMember(source);

            XMLDocNormalizer.Models.Finding finding = Assert.Single(findings);
            Assert.Equal("typeparamref", finding.TagName);
            Assert.Equal("DOC430", finding.Smell.Id);
        }
    }
}
