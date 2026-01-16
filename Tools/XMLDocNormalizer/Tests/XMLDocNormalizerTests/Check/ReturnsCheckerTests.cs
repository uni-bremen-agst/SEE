using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed <returns> and <return> XML documentation tags.
    /// Covers unclosed, mismatched, and unknown tags.
    /// </summary>
    public sealed class ReturnsCheckerTests
    {
        [Fact]
        public void Malformed_Unclosed_Returns_IsDetected()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns>Foo\n" +
                "int M() { return 0; }\n";

            string expected =
                "[4,5] <returns>: Missing end tag (unclosed XML element).";

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
                "[4,5] <return>: Unknown XML documentation tag <return>.";

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
                "[4,5] <returns>: Missing end tag (unclosed XML element).";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void Malformed_Unclosed_Returns_WithLeadingBlankLine_ReportsExpectedLocation()
        {
            string memberCode =
                "    /// <returns>Foo\n" +
                "    int M() { return 0; }\n";

            string expected =
                "[3,9] <returns>: Missing end tag (unclosed XML element).";

            CheckAssert.MemberEquals(memberCode, expected);
        }

    }
}