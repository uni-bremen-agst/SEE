using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Fix
{
    /// <summary>
    /// Unit tests for handling of <c> elements in XML documentation.
    /// </summary>
    public sealed class CTagTests
    {
        /// <summary>
        /// Unwraps <c> when it contains exactly one token.
        /// </summary>
        [Fact]
        public void C_SingleToken_IsUnwrapped()
        {
            string source =
                "/// <summary><c>GameObject</c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>GameObject</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <c> when it contains a single token even if the token is surrounded by whitespace and newlines.
        /// </summary>
        [Fact]
        public void C_SingleToken_WithNewlines_IsUnwrapped()
        {
            string source =
                "/// <summary>\n" +
                "/// <c>\n" +
                "///     null\n" +
                "/// </c>\n" +
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
        /// Unwraps <c> for a dotted identifier token.
        /// </summary>
        [Fact]
        public void C_DottedIdentifier_IsUnwrapped()
        {
            string source =
                "/// <summary><c>MyClass.MethodXYZ</c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>MyClass.MethodXYZ</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <c> for boolean literals when they are the only token.
        /// </summary>
        [Fact]
        public void C_BooleanLiteral_IsUnwrapped()
        {
            string source =
                "/// <summary>\n" +
                "/// <c>\n" +
                "///   true\n" +
                "/// </c>\n" +
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
        /// Unwraps <c> when the content contains multiple tokens.
        /// </summary>
        [Fact]
        public void C_MultipleTokens_IsUnwrapped()
        {
            string source =
                "/// <summary><c>int x = 0;</c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>int x = 0;</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        /// <summary>
        /// Unwraps <c> for a multi-line code block.
        /// </summary>
        [Fact]
        public void C_Multiline_IsPreserved()
        {
            string source =
                "/// <summary>\n" +
                "/// <c>\n" +
                "/// int x = 0;\n" +
                "/// return x;\n" +
                "/// </c>\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>\n" +
                "/// int x = 0;\n" +
                "/// return x;\n" +
                "/// </summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void C_UppercaseLiteral_IsPreserved()
        {
            string source =
                "/// <summary><c>TRUE</c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>TRUE</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void C_EmptyContent_BecomesEmptyText()
        {
            string source =
                "/// <summary><c></c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void C_NestedElement_ContentIsFlattened()
        {
            string source =
                "/// <summary><c><b>true</b></c></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary><b>true</b></summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }
    }
}
