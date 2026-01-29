using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Tests scenarios where multiple independent well-formedness issues occur in the same member.
    /// </summary>
    public sealed class WellFormed_MultipleFindingsTests
    {
        /// <summary>
        /// Ensures that mixed param and exception attribute issues are both detected.
        /// </summary>
        [Fact]
        public void Param_And_Exception_Mixed_Findings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param>Missing name</param>\n" +
                "/// <exception>Missing cref</exception>\n" +
                "void M() {}\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.HasExactlySmells(findings, "DOC300", "DOC600");
        }

        /// <summary>
        /// Ensures that multiple different issues (unknown tag + ref-tag content) are detected together.
        /// </summary>
        [Fact]
        public void Malformed_Multiple_Different_Errors_AreDetected()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "/// <returns><paramref name=\"x\">Foo</returns>\n" +
                "int M(int x) { return x; }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.ContainsSmell(findings, "DOC110");
            FindingAsserts.ContainsSmell(findings, "DOC340");
        }
    }
}
