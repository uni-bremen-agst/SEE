using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC100 (MissingDocumentation).
    /// </summary>
    public sealed class DOC100_MissingDocumentationTests
    {
        /// <summary>
        /// Ensures that a member without any XML doc comment triggers DOC100.
        /// </summary>
        [Fact]
        public void MemberWithoutDoc_IsDetected()
        {
            string member =
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicSmellsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC100", finding.Smell.Id);
            Assert.Equal("documentation", finding.TagName);
        }
    }
}
