using NUnit.Framework;
using SEE.Utils;
using System;
using System.Collections.Generic;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// Tests <see cref="SortedRanges"/>.
    /// </summary>
    internal class TestSortedRanges
    {
        [Test]
        public void TestNone()
        {
            SortedRanges sr = new();
            Assert.AreEqual(0, sr.Count);
            Assert.IsFalse(sr.TryGetValue(1, out Range _));
        }

        [Test]
        public void TestOnce()
        {
            SortedRanges sr = new();
            Range r1 = new(2, 4, new Node());
            sr.Add(r1);
            Assert.AreEqual(1, sr.Count);

            AssertContained(sr, r1);
        }

        [Test]
        public void TestTwice()
        {
            PermuteAddAndFind(new Range[] { new(2, 4, new Node()), new(7, 7, new Node()) });
        }

        [Test]
        public void TestThrice()
        {
            PermuteAddAndFind(new Range[] { new(2, 4, new Node()), new(11, 12, new Node()), new(8, 9, new Node()) });
        }

        [Test]
        public void TestFource()
        {
            PermuteAddAndFind(new Range[] { new(1, 1, new Node()), new(3, 3, new Node()),
                                            new(5, 5, new Node()), new(7, 7, new Node()) });
        }

        private static void PermuteAddAndFind(Range[] ranges)
        {
            foreach (IList<Range> permutation  in ranges.Permutations())
            {
                AssertAddAndFind(permutation);
            }
        }

        private static void AssertAddAndFind(IList<Range> ranges)
        {
            SortedRanges sr = new();

            foreach (Range r in ranges)
            {
                sr.Add(r);
            }

            Assert.AreEqual(ranges.Count, sr.Count);

            foreach (Range r in ranges)
            {
                AssertContained(sr, r);
            }
        }

        private static void AssertContained(SortedRanges sr, Range expected)
        {
            Assert.IsFalse(sr.TryGetValue(expected.Start - 1, out Range _));
            {
                for (int l = expected.Start; l <= expected.End; l++)
                {
                    Assert.IsTrue(sr.TryGetValue(l, out Range actual));
                    Assert.AreSame(expected, actual);
                }
            }
            Assert.IsFalse(sr.TryGetValue(expected.End + 1, out Range _));
        }

        [Test]
        public void TestOverlapTwice1()
        {
            PermuteOverlap(new Range[] { new(2, 7, new Node()), new(7, 7, new Node()) });
        }

        [Test]
        public void TestOverlapTwice2()
        {
            PermuteOverlap(new Range[] { new(1, 1, new Node()), new(1, 1, new Node()) });
        }

        [Test]
        public void TestOverlapThrice()
        {
            PermuteOverlap(new Range[] { new(5, 10, new Node()), new(1, 1, new Node()), new(7, 8, new Node()) });
        }

        [Test]
        public void TestOverlapFource()
        {
            PermuteOverlap(new Range[] { new(5, 10, new Node()), new(10, 15, new Node()),
                                         new(16, 20, new Node()), new(8, 12, new Node()) });
        }

        private static void PermuteOverlap(Range[] ranges)
        {
            foreach (IList<Range> permut in ranges.Permutations())
            {
                AssertOverlap(permut);
            }
        }

        private static void AssertOverlap(IList<Range> ranges)
        {
            SortedRanges sr = new();

            Assert.Throws<ArgumentException>(() =>
            {
                foreach (Range r in ranges)
                {
                    sr.Add(r);
                }
            });
        }
    }
}
