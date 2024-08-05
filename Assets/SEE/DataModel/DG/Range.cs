using System;
using System.Collections.Generic;
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
    public record Range(int StartLine, int EndLine, int? StartCharacter = null, int? EndCharacter = null) : IComparable<Range>
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
        /// Whether this range has a character range.
        /// If this is false, the range only contains full lines.
        /// </summary>
        public bool HasCharacter => StartCharacter.HasValue || EndCharacter.HasValue;

        /// <summary>
        /// Splits this range into individual lines.
        /// The resulting ranges, taken together, will cover the same area as this range,
        /// but each range will only cover a single line at most.
        /// </summary>
        /// <returns>An enumerable of ranges, each covering a single line.</returns>
        public IEnumerable<Range> SplitIntoLines()
        {
            if (Lines <= 1)
            {
                // Just the one line.
                yield return this;
            }
            else if (HasCharacter)
            {
                // If we have characters, we need to split the first and last line.
                yield return this with { EndLine = StartLine + 1, EndCharacter = null };
                for (int line = StartLine + 1; line < EndLine; line++)
                {
                    yield return new Range(line, line + 1);
                }
                yield return this with { StartLine = EndLine - 1, StartCharacter = 0 };
            }
            else
            {
                // If we don't have characters, we can just return the full lines.
                for (int line = StartLine; line < EndLine; line++)
                {
                    yield return new Range(line, line + 1);
                }
            }
        }

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

        /// <summary>
        /// Whether this range overlaps with the given line.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if this range overlaps with the given line.</returns>
        public bool Overlaps(int line)
        {
            return line >= StartLine && line < EndLine;
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
            if (lspRange == null)
            {
                return null;
            }
            return new Range(lspRange.Start.Line+1, lspRange.End.Line+1,
                             lspRange.Start.Character+1, lspRange.End.Character+1);
        }

        /// <summary>
        /// Compares this range to the given <paramref name="other"/> range.
        ///
        /// Note that this comparison is not transitive, since we do not know the length of each line!
        /// For the same reason, a result of 0 does not necessarily mean that the ranges are equal, it
        /// can also mean that we cannot tell which of the two ranges is larger.
        /// </summary>
        /// <param name="other">The range to compare to.</param>
        /// <returns>The result of the comparison.</returns>
        /// <seealso cref="IComparable{T}.CompareTo"/>
        public int CompareTo(Range other)
        {
            if (ReferenceEquals(this, other) || Equals(this, other))
            {
                return 0;
            }
            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            int lineComparison = Lines.CompareTo(other.Lines);
            if (lineComparison != 0)
            {
                return lineComparison;
            }

            // Ranges are only comparable at the character-level if they start on the same line,
            // since we do not know the length of each line.
            if (StartLine == other.StartLine)
            {
                if (StartCharacter.HasValue && other.StartCharacter.HasValue)
                {
                    Assert.IsTrue(EndCharacter.HasValue && other.EndCharacter.HasValue);
                    if (EndLine == other.EndLine)
                    {
                        // We can just count and compare the characters.
                        int characters = EndCharacter.Value - StartCharacter.Value;
                        int otherCharacters = other.EndCharacter.Value - other.StartCharacter.Value;
                        return characters.CompareTo(otherCharacters);
                    }
                    // Only other two possibilities (since lineComparison == 0):
                    // A) This line has EndCharacter 0 and this.EndLine == other.EndLine+1.
                    else if (EndLine == other.EndLine + 1 && StartCharacter < other.StartCharacter)
                    {
                        return 1;
                    }
                    // B) The other line has EndCharacter 0 and other.EndLine == this.EndLine+1.
                    else if (other.EndLine == EndLine + 1 && StartCharacter > other.StartCharacter)
                    {
                        return -1;
                    }
                    // Otherwise, we cannot tell.
                }
                else if (StartCharacter is > 0)
                {
                    // The other range is a full line, this one is not.
                    return -1;
                }
                else if (other.StartCharacter is > 0 || (!EndCharacter.HasValue && other.EndLine < EndLine))
                {
                     // This range is a full line, the other one either is not or is shorter.
                    return 1;
                }
                else if (!other.EndCharacter.HasValue && EndLine < other.EndLine)
                {
                    // This range is a full line, the other one is not.
                    return -1;
                }
            }

            return 0;
        }
    }
}
