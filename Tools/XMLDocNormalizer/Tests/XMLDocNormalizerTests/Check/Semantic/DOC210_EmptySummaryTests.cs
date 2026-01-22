using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC210 (EmptySummary).
    /// </summary>
    public sealed class DOC210_EmptySummaryTests
    {
        /// <summary>
        /// Ensures that an empty summary triggers DOC210.
        /// </summary>
        [Fact]
        public void EmptySummary_IsDetected()
        {
            string member =
                "/// <summary>\n" +
                "/// \n" +
                "/// </summary>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindBasicSmellsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC210", finding.Smell.Id);
            Assert.Equal("summary", finding.TagName);
        }
    }
}
