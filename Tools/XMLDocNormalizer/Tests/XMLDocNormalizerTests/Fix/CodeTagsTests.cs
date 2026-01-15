using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Fix
{
    /// <summary>
    /// Unit tests for handling of <code> elements in XML documentation.
    /// </summary>
    public sealed class CodeTagTests
    {
        /// <summary>
        /// Unwraps <code> when it contains exactly one token.
        /// </summary>
        [Fact]
        public void Code_SingleToken_IsUnwrapped()
        {
            string source =
                "/// <summary><code>GameObject</code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>GameObject</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <code> when it contains a single token even if the token is surrounded by whitespace and newlines.
        /// </summary>
        [Fact]
        public void Code_SingleToken_WithNewlines_IsUnwrapped()
        {
            string source =
                "/// <summary>\n" +
                "/// <code>\n" +
                "///     null\n" +
                "/// </code>\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>\n" +
                "/// null\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <code> for a dotted identifier token.
        /// </summary>
        [Fact]
        public void Code_DottedIdentifier_IsUnwrapped()
        {
            string source =
                "/// <summary><code>MyClass.MethodXYZ</code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>MyClass.MethodXYZ</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <code> for boolean literals when they are the only token.
        /// </summary>
        [Fact]
        public void Code_BooleanLiteral_IsUnwrapped()
        {
            string source =
                "/// <summary>\n" +
                "/// <code>\n" +
                "///   true\n" +
                "/// </code>\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>\n" +
                "/// true\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Preserves <code> when the content contains multiple tokens.
        /// </summary>
        [Fact]
        public void Code_MultipleTokens_IsPreserved()
        {
            string source =
                "/// <summary><code>int x = 0;</code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary><code>int x = 0;</code></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Preserves <code> for a multi-line code block.
        /// </summary>
        [Fact]
        public void Code_Multiline_IsPreserved()
        {
            string source =
                "/// <summary>\n" +
                "/// <code>\n" +
                "/// int x = 0;\n" +
                "/// return x;\n" +
                "/// </code>\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>\n" +
                "/// <code>\n" +
                "/// int x = 0;\n" +
                "/// return x;\n" +
                "/// </code>\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void Code_UppercaseLiteral_IsPreserved()
        {
            string source =
                "/// <summary><code>TRUE</code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>TRUE</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void Code_EmptyContent_IsPreservedAsCode()
        {
            string source =
                "/// <summary><code></code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void Code_NestedElement_ContentIsFlattened()
        {
            string source =
                "/// <summary><code><b>false</b></code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary><b>false</b></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void Code_MultipleTokensWithWhitespace_ArePreserved()
        {
            string source =
                "/// <summary><code>true  false</code></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary><code>true  false</code></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }
    }
}
