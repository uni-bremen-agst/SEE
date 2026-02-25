using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Calculates SLOC (source lines of code) for a syntax tree.
    /// </summary>
    /// <remarks>
    /// This implementation approximates common SLOC tools (e.g. scc) by counting lines that contain
    /// any code after ignoring comments, while avoiding false positives for comment markers inside
    /// string and character literals.
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
        /// The number of non-empty lines that contain code outside of comments.
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
                string lineText = line.ToString();

                if (LineContainsCode(lineText, ref inBlockComment))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines whether a single line contains code outside of comments.
        /// </summary>
        /// <param name="line">
        /// The original line text.
        /// </param>
        /// <param name="inBlockComment">
        /// Tracks whether scanning starts inside a block comment from a previous line.
        /// The value is updated if the line opens or closes a block comment.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the line contains code; otherwise <see langword="false"/>.
        /// </returns>
        private static bool LineContainsCode(string line, ref bool inBlockComment)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            bool codeSeen = false;

            bool inString = false;
            bool inVerbatimString = false;
            bool inChar = false;
            bool escape = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inBlockComment)
                {
                    if (c == '*' && i + 1 < line.Length && line[i + 1] == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (inString)
                {
                    if (inVerbatimString)
                    {
                        if (c == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                i++;
                                continue;
                            }

                            inString = false;
                            inVerbatimString = false;
                        }

                        continue;
                    }

                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (inChar)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (c == '\'')
                    {
                        inChar = false;
                    }

                    continue;
                }

                // Not inside string/char/comment: detect comment starts.
                if (c == '/' && i + 1 < line.Length)
                {
                    char next = line[i + 1];

                    if (next == '/')
                    {
                        return codeSeen;
                    }

                    if (next == '*')
                    {
                        inBlockComment = true;
                        i++;
                        continue;
                    }
                }

                // Detect string start (including verbatim @"...") and mark as code.
                if (c == '"')
                {
                    codeSeen = true;
                    inString = true;
                    inVerbatimString = IsVerbatimStringStart(line, i);
                    continue;
                }

                // Detect char literal start and mark as code.
                if (c == '\'')
                {
                    codeSeen = true;
                    inChar = true;
                    continue;
                }

                // Any non-whitespace character outside comments counts as code.
                if (!char.IsWhiteSpace(c))
                {
                    codeSeen = true;
                }
            }

            return codeSeen;
        }

        /// <summary>
        /// Determines whether a quote character at the specified position starts a verbatim string.
        /// </summary>
        /// <param name="line">The line text.</param>
        /// <param name="quoteIndex">The index of the quote character.</param>
        /// <returns>
        /// <see langword="true"/> if the quote is preceded by '@' (ignoring whitespace is not applied); otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsVerbatimStringStart(string line, int quoteIndex)
        {
            if (quoteIndex <= 0)
            {
                return false;
            }

            return line[quoteIndex - 1] == '@';
        }
    }
}