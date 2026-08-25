using NUnit.Framework;
using static SEE.DataModel.DG.RangeExamples;

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
            Assert.That(OneFullLine.Lines, Is.EqualTo(1));
            Assert.That(TwoFullLines.Lines, Is.EqualTo(2));
            Assert.That(OneCharacter.Lines, Is.EqualTo(1));
            Assert.That(HalfALineStart.Lines, Is.EqualTo(1));
            Assert.That(HalfALineEnd.Lines, Is.EqualTo(1));
            Assert.That(OneAndAHalfEndLine.Lines, Is.EqualTo(2));
            Assert.That(OneAndAHalfStartLine.Lines, Is.EqualTo(2));
            Assert.That(OneAndTwoHalfLines.Lines, Is.EqualTo(3));
            Assert.That(LargeRange.Lines, Is.EqualTo(100));
            Assert.That(LargeLineRange.Lines, Is.EqualTo(301));
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
            Assert.That(ComparisonMatrix.GetLength(0), Is.EqualTo(AllRanges.Count),
                        "The comparison matrix must have one row per example range.");
            Assert.That(ComparisonMatrix.Rank, Is.EqualTo(2),
                        "The comparison matrix must be two-dimensional.");

            for (int i = 0; i < ComparisonMatrix.GetLength(0); i++)
            {
                Assert.That(ComparisonMatrix.GetLength(1), Is.EqualTo(AllRanges.Count),
                            "The comparison matrix must have one column per example range.");
                for (int j = 0; j < ComparisonMatrix.GetLength(1); j++)
                {
                    Range firstRange = AllRanges[i];
                    Range secondRange = AllRanges[j];
                    int comparison = ComparisonMatrix[i, j];
                    int actual = firstRange.CompareTo(secondRange);
                    Assert.That(actual, Is.EqualTo(comparison),
                                $"Expected {firstRange} {ComparisonToSymbol(comparison)} "
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
            Assert.That(OneFullLine.Contains(0, 0), Is.False, $"{OneFullLine} must not contain 0:0.");
            Assert.That(OneFullLine.Contains(0, 5), Is.False, $"{OneFullLine} must not contain 0:5.");
            Assert.That(OneFullLine.Contains(1, 0), Is.True, $"{OneFullLine} must contain 1:0.");
            Assert.That(OneFullLine.Contains(1, 1), Is.True, $"{OneFullLine} must contain 1:1.");
            Assert.That(OneFullLine.Contains(1, 10), Is.True, $"{OneFullLine} must contain 1:10.");
            Assert.That(OneFullLine.Contains(2, 0), Is.False, $"{OneFullLine} must not contain 2:0.");

            Assert.That(OneFullLine.Contains(0, 0), Is.False, $"{OneFullLine} must not contain 0:0.");
            Assert.That(OneFullLine.Contains(0, 5), Is.False, $"{OneFullLine} must not contain 0:5.");
            Assert.That(TwoFullLines.Contains(1, 0), Is.True, $"{TwoFullLines} must contain 1:0.");
            Assert.That(TwoFullLines.Contains(1, 1), Is.True, $"{TwoFullLines} must contain 1:1.");
            Assert.That(TwoFullLines.Contains(1, 10), Is.True, $"{TwoFullLines} must contain 1:10.");
            Assert.That(TwoFullLines.Contains(2, 0), Is.True, $"{TwoFullLines} must contain 2:0.");
            Assert.That(TwoFullLines.Contains(2, 5), Is.True, $"{TwoFullLines} must contain 2:5.");
            Assert.That(TwoFullLines.Contains(3, 0), Is.False, $"{TwoFullLines} must not contain 3:0.");
            Assert.That(OneFullLine.Contains(3, 5), Is.False, $"{OneFullLine} must not contain 3:5.");

            Assert.That(OneCharacter.Contains(0, 0), Is.False, $"{OneCharacter} must not contain 0:0.");
            Assert.That(OneCharacter.Contains(1, 1), Is.False, $"{OneCharacter} must not contain 1:1.");
            Assert.That(OneCharacter.Contains(1, 2), Is.True, $"{OneCharacter} must contain 1:2.");
            Assert.That(OneCharacter.Contains(1, 3), Is.False, $"{OneCharacter} must not contain 1:3.");
            Assert.That(OneCharacter.Contains(1, 4), Is.False, $"{OneCharacter} must not contain 1:4.");
            Assert.That(OneCharacter.Contains(2, 0), Is.False, $"{OneCharacter} must not contain 2:0.");

            Assert.That(HalfALineStart.Contains(0, 0), Is.False, $"{HalfALineStart} must not contain 0:0.");
            Assert.That(HalfALineStart.Contains(0, 5), Is.False, $"{HalfALineStart} must not contain 0:5.");
            Assert.That(HalfALineStart.Contains(1, 0), Is.True, $"{HalfALineStart} must contain 1:0.");
            Assert.That(HalfALineStart.Contains(1, 1), Is.True, $"{HalfALineStart} must contain 1:1.");
            Assert.That(HalfALineStart.Contains(1, 3), Is.True, $"{HalfALineStart} must contain 1:3.");
            Assert.That(HalfALineStart.Contains(1, 4), Is.False, $"{HalfALineStart} must not contain 1:4.");
            Assert.That(HalfALineStart.Contains(2, 0), Is.False, $"{HalfALineStart} must not contain 2:0.");

            Assert.That(HalfALineEnd.Contains(0, 0), Is.False, $"{HalfALineEnd} must not contain 0:0.");
            Assert.That(HalfALineEnd.Contains(0, 5), Is.False, $"{HalfALineEnd} must not contain 0:5.");
            Assert.That(HalfALineEnd.Contains(1, 4), Is.False, $"{HalfALineEnd} must not contain 1:4.");
            Assert.That(HalfALineEnd.Contains(1, 5), Is.True, $"{HalfALineEnd} must contain 1:5.");
            Assert.That(HalfALineEnd.Contains(2, 0), Is.False, $"{HalfALineEnd} must not contain 2:0.");
            Assert.That(HalfALineEnd.Contains(2, 1), Is.False, $"{HalfALineEnd} must not contain 2:1.");

            Assert.That(OneAndAHalfEndLine.Contains(1, 0), Is.True, $"{OneAndAHalfEndLine} must contain 1:0.");
            Assert.That(OneAndAHalfEndLine.Contains(1, 5), Is.True, $"{OneAndAHalfEndLine} must contain 1:5.");
            Assert.That(OneAndAHalfEndLine.Contains(1, 6), Is.True, $"{OneAndAHalfEndLine} must contain 1:6.");
            Assert.That(OneAndAHalfEndLine.Contains(1, 7), Is.True, $"{OneAndAHalfEndLine} must contain 1:7.");
            Assert.That(OneAndAHalfEndLine.Contains(2, 0), Is.True, $"{OneAndAHalfEndLine} must contain 2:0.");
            Assert.That(OneAndAHalfEndLine.Contains(2, 5), Is.True, $"{OneAndAHalfEndLine} must contain 2:5.");
            Assert.That(OneAndAHalfEndLine.Contains(2, 6), Is.False, $"{OneAndAHalfEndLine} must not contain 2:6.");
            Assert.That(OneAndAHalfEndLine.Contains(3, 0), Is.False, $"{OneAndAHalfEndLine} must not contain 3:0.");

            Assert.That(OneAndAHalfStartLine.Contains(1, 6), Is.False, $"{OneAndAHalfStartLine} must not contain 1:6.");
            Assert.That(OneAndAHalfStartLine.Contains(1, 7), Is.True, $"{OneAndAHalfStartLine} must contain 1:7.");
            Assert.That(OneAndAHalfStartLine.Contains(1, 9), Is.True, $"{OneAndAHalfStartLine} must contain 1:9.");
            Assert.That(OneAndAHalfStartLine.Contains(2, 0), Is.True, $"{OneAndAHalfStartLine} must contain 2:0.");
            Assert.That(OneAndAHalfStartLine.Contains(2, 9), Is.True, $"{OneAndAHalfStartLine} must contain 2:9.");
            Assert.That(OneAndAHalfStartLine.Contains(3, 0), Is.False, $"{OneAndAHalfStartLine} must not contain 3:0.");
            Assert.That(OneAndAHalfStartLine.Contains(3, 5), Is.False, $"{OneAndAHalfStartLine} must not contain 3:5.");

            Assert.That(OneAndTwoHalfLines.Contains(1, 4), Is.False, $"{OneAndTwoHalfLines} must not contain 1:4.");
            Assert.That(OneAndTwoHalfLines.Contains(1, 5), Is.True, $"{OneAndTwoHalfLines} must contain 1:5.");
            Assert.That(OneAndTwoHalfLines.Contains(1, 6), Is.True, $"{OneAndTwoHalfLines} must contain 1:6.");
            Assert.That(OneAndTwoHalfLines.Contains(2, 5), Is.True, $"{OneAndTwoHalfLines} must contain 2:5.");
            Assert.That(OneAndTwoHalfLines.Contains(2, 6), Is.True, $"{OneAndTwoHalfLines} must contain 2:6.");
            Assert.That(OneAndTwoHalfLines.Contains(3, 4), Is.True, $"{OneAndTwoHalfLines} must contain 3:4.");
            Assert.That(OneAndTwoHalfLines.Contains(3, 5), Is.False, $"{OneAndTwoHalfLines} must not contain 3:5.");

            Assert.That(LargeRange.Contains(0, 0), Is.False, $"{LargeRange} must not contain 0:0.");
            Assert.That(LargeRange.Contains(0, 50), Is.False, $"{LargeRange} must not contain 0:50.");
            Assert.That(LargeRange.Contains(1, 0), Is.False, $"{LargeRange} must not contain 1:0.");
            Assert.That(LargeRange.Contains(1, 2), Is.False, $"{LargeRange} must not contain 1:2.");
            Assert.That(LargeRange.Contains(1, 3), Is.True, $"{LargeRange} must contain 1:3.");
            Assert.That(LargeRange.Contains(1, 99), Is.True, $"{LargeRange} must contain 1:99.");
            Assert.That(LargeRange.Contains(1, 100), Is.True, $"{LargeRange} must contain 1:100.");
            Assert.That(LargeRange.Contains(1, 150), Is.True, $"{LargeRange} must contain 1:150.");
            Assert.That(LargeRange.Contains(100, 3), Is.True, $"{LargeRange} must contain 100:3.");
            Assert.That(LargeRange.Contains(100, 99), Is.True, $"{LargeRange} must contain 100:99.");
            Assert.That(LargeRange.Contains(100, 100), Is.False, $"{LargeRange} must not contain 100:100.");
            Assert.That(LargeRange.Contains(100, 150), Is.False, $"{LargeRange} must not contain 100:150.");

            Assert.That(LargeLineRange.Contains(0, 0), Is.True, $"{LargeLineRange} must contain 0:0.");
            Assert.That(LargeLineRange.Contains(0, 50), Is.True, $"{LargeLineRange} must contain 0:50.");
            Assert.That(LargeLineRange.Contains(1, 0), Is.True, $"{LargeLineRange} must contain 1:0.");
            Assert.That(LargeLineRange.Contains(1, 50), Is.True, $"{LargeLineRange} must contain 1:50.");
            Assert.That(LargeLineRange.Contains(300, 0), Is.True, $"{LargeLineRange} must contain 300:0.");
            Assert.That(LargeLineRange.Contains(300, 50), Is.True, $"{LargeLineRange} must contain 300:50.");
            Assert.That(LargeLineRange.Contains(301, 0), Is.False, $"{LargeLineRange} must not contain 301:0.");
            Assert.That(LargeLineRange.Contains(301, 50), Is.False, $"{LargeLineRange} must not contain 301:50.");
        }

        [Test]
        public void TestContainsSelf()
        {
            foreach (Range range in AllRanges)
            {
                Assert.That(range.Contains(range), Is.True, $"Range {range} should contain itself.");
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
                Assert.That(range.Contains(AllRanges[i]), Is.EqualTo(shouldContain),
                            $"Range {range} should {(shouldContain ? "" : "not ")}contain {AllRanges[i]}.");
            }
        }
    }
}
