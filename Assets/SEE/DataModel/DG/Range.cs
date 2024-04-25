using UnityEngine.Assertions;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Represents a range in a source file, going from the <see cref="StartLine"/> to the <see cref="EndLine"/>
    /// and optionally from the <see cref="StartCharacter"/> to the <see cref="EndCharacter"/>.
    ///
    /// Note that <see cref="StartLine"/> and <see cref="StartCharacter"/> are inclusive, while
    /// <see cref="EndLine"/> and <see cref="EndCharacter"/> are exclusive.
    /// </summary>
    /// <param name="StartLine">Line number of the start of the range (inclusive).</param>
    /// <param name="EndLine">Line number of the end of the range (exclusive).</param>
    /// <param name="StartCharacter">Character offset of the start of the range (inclusive).</param>
    /// <param name="EndCharacter">Character offset of the end of the range (exclusive).</param>
    public record Range(int StartLine, int EndLine, int? StartCharacter = null, int? EndCharacter = null)
    {
        /// <summary>
        /// Returns the number of lines in the range.
        /// Note that this also includes partial lines
        /// (i.e. if <see cref="StartCharacter"/> and <see cref="EndCharacter"/> are set).
        /// </summary>
        public int Lines =>
            // The last line may be a partial line and hence needs to be counted.
            EndLine - StartLine + (EndCharacter is > 0 ? 1 : 0);

        /// <summary>
        /// Returns the start line and character as a tuple.
        /// </summary>
        public (int Line, int Character) Start => (StartLine, StartCharacter ?? 0);

        /// <summary>
        /// Returns the end line and character as a tuple.
        /// </summary>
        public (int Line, int Character) End => (EndLine, EndCharacter ?? 0);

        /// <summary>
        /// Returns true if the given line and character are contained in this range.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <param name="character">The character to check. If not set, only the line is checked.</param>
        /// <returns>True if the given line and character are contained in this range.</returns>
        public bool Contains(int line, int character)
        {
            if (line == StartLine)
            {
                return character >= (StartCharacter ?? 0) && (line < EndLine || character < (EndCharacter ?? int.MaxValue));
            }
            else if (line == EndLine)
            {
                // 0 here because if no EndCharacter is set, the range ends at the end of the previous line,
                // as end indices are exclusive.
                return character < (EndCharacter ?? 0);
            }
            else
            {
                return line > StartLine && line < EndLine;
            }
        }

        /// <summary>
        /// Returns true if the given range is completely contained in this range.
        /// </summary>
        /// <param name="other">The range to check.</param>
        /// <returns>True if the given range is completely contained in this range.</returns>
        public bool Contains(Range other)
        {
            bool contains;
            if (other.StartCharacter.HasValue && other.EndCharacter.HasValue)
            {
                contains = Contains(other.StartLine, other.StartCharacter.Value);
                if (other.EndCharacter.Value > 0)
                {
                    // Must contain last line up to EndCharacter.
                    contains = contains && Contains(other.EndLine, other.EndCharacter.Value - 1);
                }
                else
                {
                    // Must contain up to last line.
                    contains = contains && Contains(other.EndLine - 1, int.MaxValue);
                }
            }
            else
            {
                Assert.IsTrue(!other.StartCharacter.HasValue && !other.EndCharacter.HasValue);
                // Has to contain all lines in full.
                contains = Contains(other.StartLine, 0) && Contains(other.StartLine, int.MaxValue);
                contains = contains && Contains(other.EndLine - 1, 0) && Contains(other.EndLine - 1, int.MaxValue);
            }
            return contains;
        }

        public override string ToString()
        {
            return StartCharacter.HasValue && EndCharacter.HasValue
                ? $"{StartLine}:{StartCharacter} – {EndLine}:{EndCharacter}"
                : $"{StartLine} – {EndLine}";
        }

        /// <summary>
        /// Converts the given <paramref name="lspRange"/> (i.e., from OmniSharp) to a <see cref="Range"/>.
        /// </summary>
        /// <param name="lspRange">The LSP range to convert.</param>
        /// <returns>The converted range.</returns>
        public static Range FromLspRange(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range lspRange)
        {
            return new Range(lspRange.Start.Line, lspRange.End.Line, lspRange.Start.Character, lspRange.End.Character);
        }
    }
}
