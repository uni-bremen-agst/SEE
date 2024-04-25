using System.Collections.Generic;
using SEE.DataModel.DG;

namespace SEETests
{
    /// <summary>
    /// Examples of ranges for testing purposes.
    /// </summary>
    internal static class RangeExamples
    {
        internal static readonly Range OneFullLine = new(1, 2);
        internal static readonly Range TwoFullLines = new(1, 3);

        internal static readonly Range OneCharacter = new(1, 1, 2, 3);
        internal static readonly Range HalfALineStart = new(1, 1, 0, 4);
        internal static readonly Range HalfALineEnd = new(1, 2, 5, 0);

        internal static readonly Range OneAndAHalfEndLine = new(1, 2, 0, 6);
        internal static readonly Range OneAndAHalfStartLine = new(1, 3, 7, 0);
        internal static readonly Range OneAndTwoHalfLines = new(1, 3, 5, 5);

        internal static readonly Range LargeRange = new(1, 100, 3, 100);
        internal static readonly Range LargeLineRange = new(0, 301);

        internal static readonly IList<Range> AllRanges = new List<Range>
        {
            OneFullLine, TwoFullLines,
            OneCharacter, HalfALineStart, HalfALineEnd,
            OneAndAHalfEndLine, OneAndAHalfStartLine, OneAndTwoHalfLines,
            LargeRange, LargeLineRange
        };

    }
}
