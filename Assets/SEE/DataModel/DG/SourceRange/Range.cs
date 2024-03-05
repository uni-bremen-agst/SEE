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
        public override string ToString()
        {
            return StartCharacter.HasValue && EndCharacter.HasValue
                ? $"{StartLine}:{StartCharacter} – {EndLine}:{EndCharacter}"
                : $"{StartLine} – {EndLine}";
        }
    }
}
