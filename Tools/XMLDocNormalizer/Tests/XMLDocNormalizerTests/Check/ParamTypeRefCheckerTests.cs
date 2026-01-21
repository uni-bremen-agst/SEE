using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed paramref and typeparamref XML documentation tags.
    /// Checks include:
    /// - paramref and typeparamref tags with content (should be empty)
    /// - Correctly formed tags (should produce no findings)
    /// </summary>
    public sealed class ParamTypeRefCheckerTests
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
                "[4,14] <paramref>: <paramref> should be an empty element, e.g. <paramref name=\"x\"/>.";

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
                "[4,14] <paramref>: <paramref> should be an empty element, e.g. <paramref name=\"x\"/>.\n" +
                "[5,9] <paramref>: <paramref> should be an empty element, e.g. <paramref name=\"x\"/>.";

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
                "[4,14] <typeparamref>: <typeparamref> should be an empty element, e.g. <typeparamref name=\"T\"/>.";

            CheckAssert.MemberEquals(source, expected);
        }

        [Fact]
        public void WellFormed_ProducesNoFindings()
        {
            string source =
                "/// <summary>Test</summary>\n" +
                "/// <returns><paramref name=\"x\"/> equals 1, <typeparamref name=\"T\"/> is valid</returns>\n" +
                "int M<T>(int x) { return x; }\n";

            string expected = string.Empty;

            CheckAssert.MemberEquals(source, expected);
        }
    }
}