using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Smoke tests ensuring that valid type parameter documentation produces no type parameter findings.
    /// </summary>
    public sealed class Syntax_NoFinding_TypeParamDetector_SmokeTests
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

        /// <summary>
        /// Ensures that a correctly referenced typeparamref tag on a method produces no type parameter findings.
        /// </summary>
        [Fact]
        public void ValidTypeParamRef_OnMethod_ProducesNoFindings()
        {
            string member =
                "/// <summary>Uses <typeparamref name=\"T\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a correctly referenced typeparamref tag on a type produces no type parameter findings.
        /// </summary>
        [Fact]
        public void ValidTypeParamRef_OnType_ProducesNoFindings()
        {
            string source =
                "/// <summary>Uses <typeparamref name=\"T\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
