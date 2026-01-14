namespace XMLDocNormalizer.Tests
{
    /// <summary>
    /// Tests for handling of <code> elements in XML documentation.
    /// </summary>
    public sealed class CodeTagTests
    {
        [Fact]
        public void Code_WithSingleToken_IsUnwrapped()
        {
            string output = RewriteHarness.RewriteMember(
                "/// <summary><code>GameObject</code></summary>\n" +
                "void M() { }\n"
            );

            Assert.DoesNotContain("<code>", output);
            Assert.Contains("GameObject", output);
        }

        [Fact]
        public void Code_WithMultipleTokens_IsPreserved()
        {
            string output = RewriteHarness.RewriteMember(
                "/// <summary><code>int x = 0;</code></summary>\n" +
                "void M() { }\n"
            );

            Assert.Contains("<code>int x = 0;</code>", output);
        }
    }
}
