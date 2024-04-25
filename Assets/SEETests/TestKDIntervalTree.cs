using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static SEETests.RangeExamples;
using Range = SEE.DataModel.DG.Range;

namespace SEE.Utils
{
    /// <summary>
    /// This tests the <see cref="KDIntervalTree{E}"/> class—specifically, stabbing queries for it.
    /// </summary>
    [TestFixture]
    public class TestKDIntervalTree
    {
        [Test]
        public void TestSelfStab()
        {
            KDIntervalTree<Range> tree = new(AllRanges, x => x);
            foreach (Range range in AllRanges)
            {
                IList<Range> stabbed = tree.Stab(range).ToList();
                Assert.That(stabbed, Is.EquivalentTo(new[] { range }));
            }
        }

        [Test]
        public void TestDoubleMinimum()
        {
            KDIntervalTree<Range> tree = new(AllRanges.Append(OneFullLine).Append(OneAndAHalfEndLine), x => x);
            foreach (Range range in AllRanges)
            {
                if (range == OneFullLine || range == OneAndAHalfEndLine)
                {
                    Assert.That(tree.Stab(range), Is.EquivalentTo(new[] { range, range }));
                }
                else
                {
                    Assert.That(tree.Stab(range), Is.EquivalentTo(new[] { range }));
                }
            }
        }

        [Test]
        public void TestLineOnly()
        {
            KDIntervalTree<Range> tree = new(new[] { OneFullLine, TwoFullLines, LargeLineRange }, x => x);
            Assert.That(tree.Stab(OneFullLine), Is.EquivalentTo(new[] { OneFullLine }));
            Assert.That(tree.Stab(TwoFullLines), Is.EquivalentTo(new[] { TwoFullLines }));
            Assert.That(tree.Stab(LargeLineRange), Is.EquivalentTo(new[] { LargeLineRange }));

            Assert.That(tree.Stab(new Range(400, 501)), Is.Empty);
            Assert.That(tree.Stab(new Range(300, 301, 0, 5)), Is.Empty);

            Assert.That(tree.Stab(new Range(0, 1)), Is.EquivalentTo(new[] { LargeLineRange }));
            Assert.That(tree.Stab(new Range(2, 3)), Is.EquivalentTo(new[] { TwoFullLines }));
            Assert.That(tree.Stab(new Range(300, 301)), Is.EquivalentTo(new[] { LargeLineRange }));

            Assert.That(tree.Stab(OneCharacter), Is.EquivalentTo(new[] { OneFullLine }));
            Assert.That(tree.Stab(HalfALineStart), Is.EquivalentTo(new[] { OneFullLine }));
            Assert.That(tree.Stab(HalfALineEnd), Is.EquivalentTo(new[] { OneFullLine }));
            Assert.That(tree.Stab(OneAndAHalfEndLine), Is.EquivalentTo(new[] { TwoFullLines }));
            Assert.That(tree.Stab(OneAndAHalfStartLine), Is.EquivalentTo(new[] { TwoFullLines }));
            Assert.That(tree.Stab(OneAndTwoHalfLines), Is.EquivalentTo(new[] { LargeLineRange }));
            Assert.That(tree.Stab(LargeRange), Is.EquivalentTo(new[] { LargeLineRange }));
        }

        [Test]
        public void TestLineAndCharacter()
        {
            KDIntervalTree<Range> tree = new(new[] { OneCharacter, HalfALineStart, HalfALineEnd, OneAndAHalfEndLine, OneAndAHalfStartLine, OneAndTwoHalfLines, LargeRange }, x => x);
            Assert.That(tree.Stab(OneCharacter), Is.EquivalentTo(new[] { OneCharacter }));
            Assert.That(tree.Stab(HalfALineStart), Is.EquivalentTo(new[] { HalfALineStart }));
            Assert.That(tree.Stab(HalfALineEnd), Is.EquivalentTo(new[] { HalfALineEnd }));
            Assert.That(tree.Stab(OneAndAHalfEndLine), Is.EquivalentTo(new[] { OneAndAHalfEndLine }));
            Assert.That(tree.Stab(OneAndAHalfStartLine), Is.EquivalentTo(new[] { OneAndAHalfStartLine }));
            Assert.That(tree.Stab(OneAndTwoHalfLines), Is.EquivalentTo(new[] { OneAndTwoHalfLines }));
            Assert.That(tree.Stab(LargeRange), Is.EquivalentTo(new[] { LargeRange }));

            Assert.That(tree.Stab(new Range(200, 201)), Is.Empty);
            Assert.That(tree.Stab(new Range(100, 101, 0, 0)), Is.Empty);
            Assert.That(tree.Stab(new Range(100, 101, 0, 4)), Is.Empty);

            Assert.That(tree.Stab(new Range(1, 1, 1, 2)), Is.EquivalentTo(new[] { HalfALineStart }));
            Assert.That(tree.Stab(new Range(1, 1, 6, 9)), Is.EquivalentTo(new[] { HalfALineEnd }));
            Assert.That(tree.Stab(new Range(2, 2, 0, 4)), Is.EquivalentTo(new[] { OneAndAHalfStartLine, OneAndAHalfEndLine }));
            Assert.That(tree.Stab(new Range(2, 2, 0, 10)), Is.EquivalentTo(new[] { OneAndAHalfStartLine }));
            Assert.That(tree.Stab(new Range(2, 3, 0, 3)), Is.EquivalentTo(new[] { OneAndTwoHalfLines }));
            Assert.That(tree.Stab(new Range(50, 70, 2, 8)), Is.EquivalentTo(new[] { LargeRange }));

            Assert.That(tree.Stab(OneFullLine), Is.EquivalentTo(new[] { OneAndAHalfEndLine }));
            Assert.That(tree.Stab(TwoFullLines), Is.Empty);
            Assert.That(tree.Stab(LargeLineRange), Is.Empty);
        }
    }
}
