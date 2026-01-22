using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Smoke tests ensuring that valid basic documentation does not produce findings.
    /// </summary>
    public sealed class Semantic_NoFinding_BasicDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a member with a non-empty summary produces no basic findings.
        /// </summary>
        [Fact]
        public void SummaryPresentAndNonEmpty_ProducesNoFindings()
        {
            string member =
                "/// <summary>Returns zero.</summary>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindBasicSmellsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a summary containing nested elements is not treated as empty.
        /// </summary>
        [Fact]
        public void SummaryWithSee_IsNotEmpty()
        {
            string member =
                "/// <summary><see cref=\"System.String\"/></summary>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindBasicSmellsForMember(member);

            Assert.Empty(findings);
        }
    }
}
