using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC200 (MissingSummary).
    /// </summary>
    public sealed class DOC200_MissingSummaryTests
    {
        /// <summary>
        /// Ensures that a documentation comment without a summary triggers DOC200.
        /// </summary>
        [Fact]
        public void DocWithoutSummary_IsDetected()
        {
            string member =
                "/// <param name=\"x\">value</param>\n" +
                "public int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindBasicSmellsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal("DOC200", finding.Smell.Id);
            Assert.Equal("summary", finding.TagName);
        }
    }
}
