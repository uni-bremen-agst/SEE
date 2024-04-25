using NUnit.Framework;
using static SEETests.RangeExamples;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit tests for the <see cref="Range"/> class.
    /// </summary>
    [TestFixture]
    public class TestRanges
    {
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

        /// <summary>
        /// A matrix describing the expected CompareTo results between all example ranges, i.e.,
        /// <c>range[row].CompareTo(range[column])</c>.
        ///
        /// Note that the matrix is skew-symmetric, i.e., <c>M = -M^T</c>.
        /// </summary>
        private static readonly int[,] ComparisonMatrix =
        {
            // ReSharper disable once CommentTypo
            //1L  2L  1C  HLS HLE OHE OHS O2H LR  LLR
            { +0, -1, +1, +1, +1, -1, -1, -1, -1, -1 }, // OneFullLine
            { +1, +0, +1, +1, +1, +1, +1, -1, -1, -1 }, // TwoFullLine
            { -1, -1, +0, -1, +0, -1, -1, -1, -1, -1 }, // OneCharacter
            { -1, -1, +1, +0, +0, -1, -1, -1, -1, -1 }, // HalfALineStart
            { -1, -1, +0, +0, +0, -1, -1, -1, -1, -1 }, // HalfALineEnd
            { +1, -1, +1, +1, +1, +0, +0, -1, -1, -1 }, // OneAndAHalfEndLine
            { +1, -1, +1, +1, +1, +0, +0, -1, -1, -1 }, // OneAndAHalfStartLine
            { +1, +1, +1, +1, +1, +1, +1, +0, -1, -1 }, // OneAndTwoHalfLines
            { +1, +1, +1, +1, +1, +1, +1, +1, +0, -1 }, // LargeRange
            { +1, +1, +1, +1, +1, +1, +1, +1, +1, +0 }, // LargeLineRange
        };

        [Test]
        public void TestCompare()
        {
            Assert.IsTrue(ComparisonMatrix.GetLength(0) == AllRanges.Count);
            Assert.IsTrue(ComparisonMatrix.Rank == 2);

            for (int i = 0; i < ComparisonMatrix.GetLength(0); i++)
            {
                Assert.IsTrue(ComparisonMatrix.GetLength(1) == AllRanges.Count);
                for (int j = 0; j < ComparisonMatrix.GetLength(1); j++)
                {
                    Range firstRange = AllRanges[i];
                    Range secondRange = AllRanges[j];
                    int comparison = ComparisonMatrix[i, j];
                    int actual = firstRange.CompareTo(secondRange);
                    Assert.AreEqual(comparison, actual, $"Expected {firstRange} {ComparisonToSymbol(comparison)} "
                                    + $"{secondRange}, but got {ComparisonToSymbol(actual)} instead.");
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="comparison"></param>
        /// <returns></returns>
        private static char ComparisonToSymbol(int comparison) =>
            comparison switch
            {
                0 => '=',
                < 0 => '<',
                _ => '>'
            };

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
