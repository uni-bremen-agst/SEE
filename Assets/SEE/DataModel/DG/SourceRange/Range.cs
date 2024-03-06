namespace SEE.DataModel.DG.SourceRange
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
        public int Lines => EndLine - StartLine;

        /// <summary>
        /// Returns true if the given line and character are contained in this range.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <param name="character">The character to check. If not set, only the line is checked.</param>
        /// <returns>True if the given line and character are contained in this range.</returns>
        public bool Contains(int line, int? character = null)
        {
            return line >= StartLine && line < EndLine
                && (!character.HasValue || (character >= StartCharacter && character < EndCharacter));
        }

        /// <summary>
        /// Returns true if the given range is completely contained in this range.
        /// </summary>
        /// <param name="other">The range to check.</param>
        /// <returns>True if the given range is completely contained in this range.</returns>
        public bool Contains(Range other)
        {
            return Contains(other.StartLine, other.StartCharacter)
                && Contains(other.EndLine-1, other.EndCharacter-1);  // -1 because EndLine and EndCharacter are exclusive
        }

        public override string ToString()
        {
            return StartCharacter.HasValue && EndCharacter.HasValue
                ? $"{StartLine}:{StartCharacter} – {EndLine}:{EndCharacter}"
                : $"{StartLine} – {EndLine}";
        }
    }
}
