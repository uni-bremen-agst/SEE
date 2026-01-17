using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Fix
{
    /// <summary>
    /// Tests for <see cref="XMLDocNormalizer.Rewriting.XmlDocRewriter"/> which normalizes
    /// selected XML documentation tags (<c>param</c>, <c>returns</c>, <c>exception</c>).
    /// </summary>
    public sealed class XmlDocRewriterTests
    {
        /// <summary>
        /// Ensures that the rewriter normalizes <c>&lt;param&gt;</c> by capitalizing the first letter
        /// and ensuring the content ends with a period.
        /// </summary>
        [Fact]
        public void Param_IsCapitalized_AndEndsWithPeriod()
        {
            string inputMember =
                "/// <param name=\"x\">does something</param>\n" +
                "void M(int x) { }\n";

            string expectedMember =
                "/// <param name=\"x\">Does something.</param>\n" +
                "void M(int x) { }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that the rewriter normalizes <c>&lt;returns&gt;</c> by capitalizing the first letter
        /// and ensuring the content ends with a period.
        /// </summary>
        [Fact]
        public void Returns_IsCapitalized_AndEndsWithPeriod()
        {
            string inputMember =
                "/// <returns>returns a value</returns>\n" +
                "int M() { return 0; }\n";

            string expectedMember =
                "/// <returns>Returns a value.</returns>\n" +
                "int M() { return 0; }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that the rewriter normalizes <c>&lt;exception&gt;</c> by capitalizing the first letter
        /// and ensuring the content ends with a period.
        /// </summary>
        [Fact]
        public void Exception_IsCapitalized_AndEndsWithPeriod()
        {
            string inputMember =
                "/// <exception cref=\"System.Exception\">throws sometimes</exception>\n" +
                "void M() { }\n";

            string expectedMember =
                "/// <exception cref=\"System.Exception\">Throws sometimes.</exception>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that tags other than <c>param</c>, <c>returns</c>, and <c>exception</c>
        /// are not modified by <see cref="XMLDocNormalizer.Rewriting.XmlDocRewriter"/>.
        /// </summary>
        [Fact]
        public void Summary_IsNotModified()
        {
            string inputMember =
                "/// <summary>should stay exactly as-is</summary>\n" +
                "void M() { }\n";

            string expectedMember =
                "/// <summary>should stay exactly as-is</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that normalization is idempotent: already normalized output stays unchanged
        /// when processed again.
        /// </summary>
        [Fact]
        public void Normalization_IsIdempotent_ForParamReturnsException()
        {
            string inputMember =
                "/// <param name=\"x\">Does something.</param>\n" +
                "/// <returns>Returns a value.</returns>\n" +
                "/// <exception cref=\"System.Exception\">Throws sometimes.</exception>\n" +
                "int M(int x) { return x; }\n";

            string expectedMember =
                "/// <param name=\"x\">Does something.</param>\n" +
                "/// <returns>Returns a value.</returns>\n" +
                "/// <exception cref=\"System.Exception\">Throws sometimes.</exception>\n" +
                "int M(int x) { return x; }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }
    }
}
