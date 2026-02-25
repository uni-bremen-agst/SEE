using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Calculates SLOC (source lines of code) for a syntax tree.
    /// </summary>
    /// <remarks>
    /// This SLOC implementation is designed to approximate common tools such as <c>scc</c>:
    /// <list type="bullet">
    /// <item><description>Blank lines are ignored.</description></item>
    /// <item><description>Comment-only lines are ignored.</description></item>
    /// <item><description>Lines that contain any code after removing comments are counted.</description></item>
    /// </list>
    /// It handles:
    /// <list type="bullet">
    /// <item><description>Single-line comments (<c>//</c>) anywhere in the line.</description></item>
    /// <item><description>Block comments (<c>/* ... */</c>) starting anywhere in the line, spanning multiple lines.</description></item>
    /// <item><description>Multiple comment segments per line.</description></item>
    /// </list>
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
        /// The number of non-empty lines that contain code after removing comments.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tree"/> is <see langword="null"/>.
        /// </exception>
        public static int CalculateForTree(SyntaxTree tree)
        {
            ArgumentNullException.ThrowIfNull(tree);

            SourceText text = tree.GetText();
            int count = 0;
            bool inBlockComment = false;

            foreach (TextLine line in text.Lines)
            {
                string original = line.ToString();

                if (LineContainsCodeAfterRemovingComments(original, ref inBlockComment))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines whether a line contains code after stripping comment content.
        /// </summary>
        /// <param name="line">
        /// The original line text (not trimmed).
        /// </param>
        /// <param name="inBlockComment">
        /// Tracks whether scanning starts inside a block comment from a previous line.
        /// This value is updated if the line opens or closes a block comment.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the remaining content contains code; otherwise <see langword="false"/>.
        /// </returns>
        private static bool LineContainsCodeAfterRemovingComments(string line, ref bool inBlockComment)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            int i = 0;
            int length = line.Length;

            while (i < length)
            {
                if (inBlockComment)
                {
                    int endIndex = IndexOfBlockCommentEnd(line, i);
                    if (endIndex < 0)
                    {
                        // Entire remainder is still within a block comment.
                        return false;
                    }

                    // Continue after the closing "*/".
                    inBlockComment = false;
                    i = endIndex + 2;
                    continue;
                }

                int singleLineIndex = IndexOfSingleLineCommentStart(line, i);
                int blockStartIndex = IndexOfBlockCommentStart(line, i);

                // If a single-line comment starts before any block comment, then everything after is comment.
                if (singleLineIndex >= 0 &&
                    (blockStartIndex < 0 || singleLineIndex < blockStartIndex))
                {
                    // Check if there is any code before the "//".
                    return ContainsNonWhitespace(line, i, singleLineIndex);
                }

                // If a block comment starts next, check for code before it, then enter block comment mode.
                if (blockStartIndex >= 0)
                {
                    if (ContainsNonWhitespace(line, i, blockStartIndex))
                    {
                        return true;
                    }

                    int blockEndIndex = IndexOfBlockCommentEnd(line, blockStartIndex + 2);
                    if (blockEndIndex < 0)
                    {
                        inBlockComment = true;
                        return false;
                    }

                    // Block comment ends on the same line; continue scanning after it.
                    i = blockEndIndex + 2;
                    continue;
                }

                // No more comments from here; if anything non-whitespace remains, it's code.
                return ContainsNonWhitespace(line, i, length);
            }

            return false;
        }

        /// <summary>
        /// Finds the index of a single-line comment start (<c>//</c>) at or after the given position.
        /// </summary>
        /// <param name="line">The line to search.</param>
        /// <param name="startIndex">The start index to search from.</param>
        /// <returns>The index of <c>//</c> or -1 if not found.</returns>
        private static int IndexOfSingleLineCommentStart(string line, int startIndex)
        {
            return line.IndexOf("//", startIndex, StringComparison.Ordinal);
        }

        /// <summary>
        /// Finds the index of a block comment start (<c>/*</c>) at or after the given position.
        /// </summary>
        /// <param name="line">The line to search.</param>
        /// <param name="startIndex">The start index to search from.</param>
        /// <returns>The index of <c>/*</c> or -1 if not found.</returns>
        private static int IndexOfBlockCommentStart(string line, int startIndex)
        {
            return line.IndexOf("/*", startIndex, StringComparison.Ordinal);
        }

        /// <summary>
        /// Finds the index of a block comment end (<c>*/</c>) at or after the given position.
        /// </summary>
        /// <param name="line">The line to search.</param>
        /// <param name="startIndex">The start index to search from.</param>
        /// <returns>The index of <c>*/</c> or -1 if not found.</returns>
        private static int IndexOfBlockCommentEnd(string line, int startIndex)
        {
            return line.IndexOf("*/", startIndex, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the substring contains any non-whitespace character.
        /// </summary>
        /// <param name="line">The line text.</param>
        /// <param name="startIndex">The start index (inclusive).</param>
        /// <param name="endIndex">The end index (exclusive).</param>
        /// <returns>
        /// <see langword="true"/> if any non-whitespace character exists in the range; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsNonWhitespace(string line, int startIndex, int endIndex)
        {
            if (endIndex <= startIndex)
            {
                return false;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}