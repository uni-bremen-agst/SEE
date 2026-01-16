using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed <summary> XML documentation tags.
    /// Includes unclosed or misspelled <summary> tags.
    /// </summary>
    public sealed class SummaryCheckerTests
    {
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
        public void Malformed_Misspelled_Summary_IsDetected()
        {
            string source =
                "/// <summray>Test</summray>\n" +
                "int M() { return 0; }\n";

            string expected =
                "[4,5] <summray>: Unknown XML documentation tag <summray>.";

            CheckAssert.MemberEquals(source, expected);
        }
    }
}