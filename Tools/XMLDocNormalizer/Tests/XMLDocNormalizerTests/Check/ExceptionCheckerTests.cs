using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed <exception> XML documentation tags.
    /// Includes missing 'cref' attributes and well-formed tags.
    /// </summary>
    public sealed class ExceptionCheckerTests
    {
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
    }
}