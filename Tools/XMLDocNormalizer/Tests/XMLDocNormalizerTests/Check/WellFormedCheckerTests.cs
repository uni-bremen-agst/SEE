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
    }
}
