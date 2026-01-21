using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.WellFormed
{
    /// <summary>
    /// Smoke tests that ensure well-formed documentation does not produce findings.
    /// </summary>
    public sealed class WellFormed_NoFindings_SmokeTests
    {
        /// <summary>
        /// Ensures that valid param + exception tags produce no findings.
        /// </summary>
        [Fact]
        public void WellFormed_Param_And_Exception_ProducesNoFindings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param name=\"x\">Valid param</param>\n" +
                "/// <exception cref=\"System.Exception\">Valid exception</exception>\n" +
                "void M(int x) {}\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindMalformedForMember(source);
            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that self-closing &lt;paramref /&gt; and &lt;typeparamref /&gt; do not produce findings.
        /// </summary>
        [Fact]
        public void WellFormed_RefTags_ProduceNoFindings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"/> equals 1, <typeparamref name=\"T\"/> is valid</returns>\n" +
                "int M<T>(int x) { return x; }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindMalformedForMember(source);
            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a &lt;see /&gt; tag inside summary does not produce findings.
        /// </summary>
        [Fact]
        public void WellFormed_Summary_WithSelfClosingSee_IsNotReported()
        {
            string memberCode =
                "    /// <summary>Uses <see cref=\"T:System.String\"/>.</summary>\n" +
                "    void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindMalformedForMember(memberCode);
            Assert.Empty(findings);
        }
    }
}
