using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting multiple simultaneous XML documentation findings.
    /// Combines <param>, <exception>, <paramref>, <typeparamref> errors in one member.
    /// </summary>
    public sealed class MixedCheckerTests
    {
        [Fact]
        public void Param_And_Exception_Mixed_Findings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param>Missing name</param>\n" +
                "/// <exception>Missing cref</exception>\n" +
                "void M() {}\n";

            string expected =
                "[5,5] <param>: <param> tag is missing required 'name' attribute.\n" +
                "[6,5] <exception>: <exception> tag is missing required 'cref' attribute.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void WellFormed_Param_And_Exception_ProducesNoFindings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param name=\"x\">Valid param</param>\n" +
                "/// <exception cref=\"System.Exception\">Valid exception</exception>\n" +
                "void M(int x) {}\n";

            string expected = string.Empty;

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Multiple_Different_Errors_AreDetected()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "/// <returns><paramref name=\"x\">Foo</returns>\n" +
                "int M(int x) { return x; }\n";

            string expected =
                "[4,5] <summray>: Unknown XML documentation tag <summray>.\n" +
                "[5,15] <paramref>: This tag should be an empty element, e.g. <paramref name=\"x\"/>.";

            CheckAssert.MemberEquals(source, expected);
        }
    }
}