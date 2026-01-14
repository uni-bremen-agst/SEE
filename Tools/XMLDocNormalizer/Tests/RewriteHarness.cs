using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Rewriting;

namespace XMLDocNormalizer.Tests
{
    /// <summary>
    /// Provides a helper to run the XMLDocNormalizer rewrite pipeline on in-memory source code.
    /// </summary>
    internal static class RewriteHarness
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
        /// Wraps member code into a minimal class so the snippet becomes valid C#.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>Valid C# source containing the member.</returns>
        public static string WrapInClass(string memberCode)
        {
            return
                "class C\n" +
                "{\n" +
                memberCode +
                "\n}\n";
        }

        /// <summary>
        /// Rewrites a single member declaration by wrapping it into a minimal class.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>The rewritten full source text.</returns>
        public static string RewriteMember(string memberCode)
        {
            return Rewrite(WrapInClass(memberCode));
        }
    }
}
