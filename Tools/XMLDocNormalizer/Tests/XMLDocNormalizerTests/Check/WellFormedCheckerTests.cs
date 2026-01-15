using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed XML documentation.
    /// </summary>
    public sealed class WellFormedCheckerTests
    {
        [Fact]
        public void Malformed_Paramref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"> equals 1</returns>\n" +
                "int M(int x) { return x; }\n";

            // The exact line/column depend on the wrapper.
            // Start with dotnet test output once, then paste the expected string here.
            string expected =
                "[4,14] <paramref>: This tag should be an empty element, e.g. <paramref name=\"x\"/>.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Multiple_Paramref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"> equals 1.\n" +
                "/// and <paramref name=\"y\"> equals 0.</returns>\n" +
                "int M(int x) { return x; }\n";

            // The exact line/column depend on the wrapper.
            // Start with dotnet test output once, then paste the expected string here.
            string expected =
                "[4,14] <paramref>: This tag should be an empty element, e.g. <paramref name=\"x\"/>.\n" +
                "[5,9] <paramref>: This tag should be an empty element, e.g. <paramref name=\"x\"/>.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void WellFormed_ProducesNoFindings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"/> equals 1</returns>\n" +
                "int M(int x) { return x; }\n";

            string expected = string.Empty;

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Unclosed_Summary_IsDetected()
        {
            string source =
                "/// <summary>Test\n" +
                "int M() { return 0; }\n";

            string expected =
                "[4,5] <summary>: Missing end tag (unclosed XML element).";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Unclosed_Returns_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns>Foo\n" +
                "int M() { return 0; }\n";

            string expected =
                "[5,5] <returns>: Missing end tag (unclosed XML element).";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Return_Tag_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <return>Foo</return>\n" +
                "int M() { return 0; }\n";

            string expected =
                "[5,5] <return>: Unknown XML documentation tag <return>.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Mismatched_Returns_EndTag_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns>Foo</return>\n" +
                "int M() { return 0; }\n";

            string expected =
                "[5,5] <returns>: Missing end tag (unclosed XML element).";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Misspelled_Summary_IsDetected()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "int M() { return 0; }\n";

            string expected =
                "[4,5] <summray>: Unknown XML documentation tag <summray>.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Typeref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <remarks><typeparamref name=\"T\"> is invalid</remarks>\n" +
                "int M<T>() { return 0; }\n";

            string expected =
                "[5,15] <typeparamref>: This tag should be an empty element, e.g. <paramref name=\"x\"/>.";

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

        [Fact]
        public void Param_WithoutName_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <param>Missing name</param>\n" +
                "int M(int x) { return x; }\n";

            string expected =
                "[5,5] <param>: <param> tag is missing required 'name' attribute.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Exception_WithoutCref_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <exception>Missing cref</exception>\n" +
                "void M() {}\n";

            string expected =
                "[5,5] <exception>: <exception> tag is missing required 'cref' attribute.";

            CheckAssert.MemberEquals(source, expected);
        }

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

    }
}
