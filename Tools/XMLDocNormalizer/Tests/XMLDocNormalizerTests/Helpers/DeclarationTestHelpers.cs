namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helper functionality for documentation detector tests.
    /// </summary>
    internal static class DeclarationTestHelpers
    {
        /// <summary>
        /// Determines whether the given code snippet represents a top-level declaration
        /// that can be compiled as a full source file without wrapping it into a containing type.
        /// </summary>
        /// <param name="code">
        /// The code snippet to classify. This is expected to be either a full type/delegate declaration
        /// or a member declaration intended to be wrapped into a containing type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the snippet is a top-level declaration; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsTopLevelDeclaration(string code)
        {
            return code.Contains("public class", StringComparison.Ordinal) ||
                   code.Contains("public struct", StringComparison.Ordinal) ||
                   code.Contains("public interface", StringComparison.Ordinal) ||
                   code.Contains("public enum", StringComparison.Ordinal) ||
                   code.Contains("public delegate", StringComparison.Ordinal);
        }
    }
}
