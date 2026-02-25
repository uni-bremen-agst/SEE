using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Calculates SLOC (source lines of code) for a syntax tree.
    /// </summary>
    /// <remarks>
    /// SLOC in this context means:
    /// <list type="bullet">
    /// <item><description>Non-empty lines, and</description></item>
    /// <item><description>Lines that are not pure comment lines.</description></item>
    /// </list>
    /// This is a pragmatic metric intended for relative comparisons (e.g. findings per 1000 SLOC).
    /// </remarks>
    internal static class SlocCalculator
    {
        /// <summary>
        /// Calculates the SLOC count for the specified syntax tree.
        /// </summary>
        /// <param name="tree">
        /// The syntax tree to analyze.
        /// </param>
        /// <returns>
        /// The number of non-empty, non-comment lines for the tree.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tree"/> is <see langword="null"/>.
        /// </exception>
        public static int CalculateForTree(SyntaxTree tree)
        {
            ArgumentNullException.ThrowIfNull(tree);

            SourceText text = tree.GetText();
            int count = 0;

            foreach (TextLine line in text.Lines)
            {
                string trimmed = line.ToString().Trim();

                if (IsIgnorableLine(trimmed))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        /// <summary>
        /// Determines whether the specified line should be ignored for SLOC counting.
        /// </summary>
        /// <param name="trimmedLine">
        /// The trimmed line content.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the line is empty or a pure comment line; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsIgnorableLine(string trimmedLine)
        {
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                return true;
            }

            if (IsPureCommentLine(trimmedLine))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified trimmed line is a pure comment line.
        /// </summary>
        /// <param name="trimmedLine">
        /// The trimmed line content.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the line appears to consist only of comment text; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPureCommentLine(string trimmedLine)
        {
            if (trimmedLine.StartsWith("//", StringComparison.Ordinal))
            {
                return true;
            }

            if (trimmedLine.StartsWith("/*", StringComparison.Ordinal) &&
                trimmedLine.EndsWith("*/", StringComparison.Ordinal))
            {
                return true;
            }

            if (trimmedLine.StartsWith("*", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}