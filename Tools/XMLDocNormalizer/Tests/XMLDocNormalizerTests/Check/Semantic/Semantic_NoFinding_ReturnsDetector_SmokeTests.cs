using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Smoke tests ensuring that valid returns documentation produces no returns findings.
    /// </summary>
    public sealed class Semantic_NoFinding_ReturnsDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a correctly documented non-void method produces no returns smells.
        /// </summary>
        [Fact]
        public void ValidReturnsDocs_ProduceNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <returns>Ok.</returns>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
