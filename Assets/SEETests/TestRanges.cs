using System.Collections.Generic;
using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit tests for the <see cref="Range"/> class.
    /// </summary>
    [TestFixture]
    public class TestRanges
    {
        private static readonly Range OneFullLine = new(1, 2);
        private static readonly Range TwoFullLines = new(1, 3);

        private static readonly Range OneCharacter = new(1, 1, 2, 3);
        private static readonly Range HalfALineStart = new(1, 1, 0, 4);
        private static readonly Range HalfALineEnd = new(1, 2, 5, 0);

        private static readonly Range OneAndAHalfEndLine = new(1, 2, 0, 6);
        private static readonly Range OneAndAHalfStartLine = new(1, 3, 7, 0);
        private static readonly Range OneAndTwoHalfLines = new(1, 3, 5, 5);

        private static readonly Range LargeRange = new(1, 100, 3, 100);
        private static readonly Range LargeLineRange = new(0, 301);

        private static readonly IList<Range> AllRanges = new List<Range>
        {
            OneFullLine, TwoFullLines,
            OneCharacter, HalfALineStart, HalfALineEnd,
            OneAndAHalfEndLine, OneAndAHalfStartLine, OneAndTwoHalfLines,
            LargeRange, LargeLineRange
        };

        [Test]
        public void TestLines()
        {
            Assert.AreEqual(1, OneFullLine.Lines);
            Assert.AreEqual(2, TwoFullLines.Lines);
            Assert.AreEqual(1, OneCharacter.Lines);
            Assert.AreEqual(1, HalfALineStart.Lines);
            Assert.AreEqual(1, HalfALineEnd.Lines);
            Assert.AreEqual(2, OneAndAHalfEndLine.Lines);
            Assert.AreEqual(2, OneAndAHalfStartLine.Lines);
            Assert.AreEqual(3, OneAndTwoHalfLines.Lines);
            Assert.AreEqual(100, LargeRange.Lines);
            Assert.AreEqual(301, LargeLineRange.Lines);
        }

        [Test]
        public void TestContainsPoint()
        {
            Assert.IsFalse(OneFullLine.Contains(0, 0));
            Assert.IsFalse(OneFullLine.Contains(0, 5));
            Assert.IsTrue(OneFullLine.Contains(1, 0));
            Assert.IsTrue(OneFullLine.Contains(1, 1));
            Assert.IsTrue(OneFullLine.Contains(1, 10));
            Assert.IsFalse(OneFullLine.Contains(2, 0));

            Assert.IsFalse(OneFullLine.Contains(0, 0));
            Assert.IsFalse(OneFullLine.Contains(0, 5));
            Assert.IsTrue(TwoFullLines.Contains(1, 0));
            Assert.IsTrue(TwoFullLines.Contains(1, 1));
            Assert.IsTrue(TwoFullLines.Contains(1, 10));
            Assert.IsTrue(TwoFullLines.Contains(2, 0));
            Assert.IsTrue(TwoFullLines.Contains(2, 5));
            Assert.IsFalse(TwoFullLines.Contains(3, 0));
            Assert.IsFalse(OneFullLine.Contains(3, 5));

            Assert.IsFalse(OneCharacter.Contains(0, 0));
            Assert.IsFalse(OneCharacter.Contains(1, 1));
            Assert.IsTrue(OneCharacter.Contains(1, 2));
            Assert.IsFalse(OneCharacter.Contains(1, 3));
            Assert.IsFalse(OneCharacter.Contains(1, 4));
            Assert.IsFalse(OneCharacter.Contains(2, 0));

            Assert.IsFalse(HalfALineStart.Contains(0, 0));
            Assert.IsFalse(HalfALineStart.Contains(0, 5));
            Assert.IsTrue(HalfALineStart.Contains(1, 0));
            Assert.IsTrue(HalfALineStart.Contains(1, 1));
            Assert.IsTrue(HalfALineStart.Contains(1, 3));
            Assert.IsFalse(HalfALineStart.Contains(1, 4));
            Assert.IsFalse(HalfALineStart.Contains(2, 0));

            Assert.IsFalse(HalfALineEnd.Contains(0, 0));
            Assert.IsFalse(HalfALineEnd.Contains(0, 5));
            Assert.IsFalse(HalfALineEnd.Contains(1, 4));
            Assert.IsTrue(HalfALineEnd.Contains(1, 5));
            Assert.IsFalse(HalfALineEnd.Contains(2, 0));
            Assert.IsFalse(HalfALineEnd.Contains(2, 1));

            Assert.IsTrue(OneAndAHalfEndLine.Contains(1, 0));
            Assert.IsTrue(OneAndAHalfEndLine.Contains(1, 5));
            Assert.IsTrue(OneAndAHalfEndLine.Contains(1, 6));
            Assert.IsTrue(OneAndAHalfEndLine.Contains(1, 7));
            Assert.IsTrue(OneAndAHalfEndLine.Contains(2, 0));
            Assert.IsTrue(OneAndAHalfEndLine.Contains(2, 5));
            Assert.IsFalse(OneAndAHalfEndLine.Contains(2, 6));
            Assert.IsFalse(OneAndAHalfEndLine.Contains(3, 0));

            Assert.IsFalse(OneAndAHalfStartLine.Contains(1, 6));
            Assert.IsTrue(OneAndAHalfStartLine.Contains(1, 7));
            Assert.IsTrue(OneAndAHalfStartLine.Contains(1, 9));
            Assert.IsTrue(OneAndAHalfStartLine.Contains(2, 0));
            Assert.IsTrue(OneAndAHalfStartLine.Contains(2, 9));
            Assert.IsFalse(OneAndAHalfStartLine.Contains(3, 0));
            Assert.IsFalse(OneAndAHalfStartLine.Contains(3, 5));

            Assert.IsFalse(OneAndTwoHalfLines.Contains(1, 4));
            Assert.IsTrue(OneAndTwoHalfLines.Contains(1, 5));
            Assert.IsTrue(OneAndTwoHalfLines.Contains(1, 6));
            Assert.IsTrue(OneAndTwoHalfLines.Contains(2, 5));
            Assert.IsTrue(OneAndTwoHalfLines.Contains(2, 6));
            Assert.IsTrue(OneAndTwoHalfLines.Contains(3, 4));
            Assert.IsFalse(OneAndTwoHalfLines.Contains(3, 5));

            Assert.IsFalse(LargeRange.Contains(0, 0));
            Assert.IsFalse(LargeRange.Contains(0, 50));
            Assert.IsFalse(LargeRange.Contains(1, 0));
            Assert.IsFalse(LargeRange.Contains(1, 2));
            Assert.IsTrue(LargeRange.Contains(1, 3));
            Assert.IsTrue(LargeRange.Contains(1, 99));
            Assert.IsTrue(LargeRange.Contains(1, 100));
            Assert.IsTrue(LargeRange.Contains(1, 150));
            Assert.IsTrue(LargeRange.Contains(100, 3));
            Assert.IsTrue(LargeRange.Contains(100, 99));
            Assert.IsFalse(LargeRange.Contains(100, 100));
            Assert.IsFalse(LargeRange.Contains(100, 150));

            Assert.IsTrue(LargeLineRange.Contains(0, 0));
            Assert.IsTrue(LargeLineRange.Contains(0, 50));
            Assert.IsTrue(LargeLineRange.Contains(1, 0));
            Assert.IsTrue(LargeLineRange.Contains(1, 50));
            Assert.IsTrue(LargeLineRange.Contains(300, 0));
            Assert.IsTrue(LargeLineRange.Contains(300, 50));
            Assert.IsFalse(LargeLineRange.Contains(301, 0));
            Assert.IsFalse(LargeLineRange.Contains(301, 50));
        }

        [Test]
        public void TestContainsSelf()
        {
            foreach (Range range in AllRanges)
            {
                Assert.IsTrue(range.Contains(range), $"Range {range} should contain itself.");
            }
        }

        private static (Range range, int containsBitmask)[] ContainsData =
        {
            (OneFullLine, 0b10_111_000_00),
            (TwoFullLines, 0b11_111_110_00),
            (OneCharacter, 0b00_100_000_00),
            (HalfALineStart, 0b00_110_000_00),
            (HalfALineEnd, 0b00_001_000_00),
            (OneAndAHalfEndLine, 0b10_111_100_00),
            (OneAndAHalfStartLine, 0b00_000_010_00),
            (OneAndTwoHalfLines, 0b00_001_011_00),
            (LargeRange, 0b00_001_011_10),
            (LargeLineRange, 0b11_111_111_11)
        };

        [Test, TestCaseSource(nameof(ContainsData))]
        public void TestContainsOther((Range, int) data)
        {
            (Range range, int containsBitmask) = data;
            for (int i = 0; i < AllRanges.Count; i++)
            {
                bool shouldContain = (containsBitmask & (1 << (AllRanges.Count - 1 - i))) != 0;
                Assert.AreEqual(shouldContain, range.Contains(AllRanges[i]),
                                $"Range {range} should {(shouldContain ? "" : "not ")}contain {AllRanges[i]}.");
            }
        }
    }
}
