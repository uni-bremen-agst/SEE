using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Rewriting;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helpers for asserting rewrite behavior using full-string equality comparisons.
    /// </summary>
    internal static class RewriteAssert
    {
        /// <summary>
        /// Runs the same rewrite pipeline as the CLI tool on the given source text.
        /// </summary>
        /// <param name="source">The C# source code to rewrite.</param>
        /// <returns>The rewritten source code.</returns>
        public static string Rewrite(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            SyntaxNode root = tree.GetRoot();

            SyntaxNode afterLiteralFix = new LiteralRefactorer().Visit(root);
            SyntaxNode afterDocFix = new XmlDocRewriter().Visit(afterLiteralFix);

            return afterDocFix.ToFullString();
        }

        /// <summary>
        /// Rewrites a member snippet by wrapping it into a minimal class and running the tool pipeline.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>The rewritten full source text.</returns>
        public static string RewriteMember(string memberCode)
        {
            string source = Wrapper.WrapInClass(memberCode);
            return Rewrite(source);
        }

        /// <summary>
        /// Rewrites the given input member and asserts that the result equals the expected member rewrite.
        /// </summary>
        /// <param name="inputMember">The input member code.</param>
        /// <param name="expectedMember">The expected member code after rewriting.</param>
        public static void MemberEquals(string inputMember, string expectedMember)
        {
            string actual = RewriteMember(inputMember);
            string expected = Wrapper.WrapInClass(expectedMember);

            Assert.Equal(NormalizeNewlines(expected), NormalizeNewlines(actual));
        }

        /// <summary>
        /// Normalizes newlines to avoid platform-specific differences in test output.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>The text with normalized newlines.</returns>
        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}