namespace XMLDocNormalizerTests.Check
{
    /// <summary>
    /// Tests for detecting malformed <param> XML documentation tags.
    /// Includes missing 'name' attributes and well-formed tags.
    /// </summary>
    public sealed class ParamCheckerTests
    {
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
    }
}