using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Smoke tests ensuring that valid parameter documentation produces no parameter findings.
    /// </summary>
    public sealed class Syntax_NoFinding_ParamDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a correctly documented method produces no DOC310/DOC320/DOC330/DOC350 findings.
        /// </summary>
        [Fact]
        public void ValidParamDocs_ForMethod_ProduceNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "/// <param name=\"y\">y</param>\n" +
                "public void M(int x, int y) { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindParamFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a correctly documented indexer produces no parameter findings.
        /// </summary>
        [Fact]
        public void ValidParamDocs_ForIndexer_ProduceNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindParamFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
