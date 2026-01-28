using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Smoke tests ensuring that valid type parameter documentation produces no type parameter findings.
    /// </summary>
    public sealed class Semantic_NoFinding_TypeParamDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that correctly documented type parameters produce no DOC410/DOC420/DOC430/DOC450 findings.
        /// </summary>
        [Fact]
        public void ValidTypeParamDocs_ProduceNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "public void M<T, U>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
