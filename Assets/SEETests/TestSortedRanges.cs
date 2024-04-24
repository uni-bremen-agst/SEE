using NUnit.Framework;
using SEE.Utils;
using System;
using System.Collections.Generic;
using SEE.DataModel.DG.GraphIndex;

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
            Assert.IsFalse(sr.TryGetValue(1, out GraphIndex.SourceRange _));
        }

        [Test]
        public void TestOnce()
        {
            SortedRanges sr = new();
            GraphIndex.SourceRange r1 = new(2, 4, new Node());
            sr.Add(r1);
            Assert.AreEqual(1, sr.Count);

            AssertContained(sr, r1);
        }

        [Test]
        public void TestTwice()
        {
            PermuteAddAndFind(new GraphIndex.SourceRange[] { new(2, 4, new Node()), new(7, 7, new Node()) });
        }

        [Test]
        public void TestThrice()
        {
            PermuteAddAndFind(new GraphIndex.SourceRange[] { new(2, 4, new Node()), new(11, 12, new Node()), new(8, 9, new Node()) });
        }

        [Test]
        public void TestFource()
        {
            PermuteAddAndFind(new GraphIndex.SourceRange[] { new(1, 1, new Node()), new(3, 3, new Node()),
                                            new(5, 5, new Node()), new(7, 7, new Node()) });
        }

        private static void PermuteAddAndFind(GraphIndex.SourceRange[] ranges)
        {
            foreach (IList<GraphIndex.SourceRange> permutation  in ranges.Permutations())
            {
                AssertAddAndFind(permutation);
            }
        }

        private static void AssertAddAndFind(IList<GraphIndex.SourceRange> ranges)
        {
            SortedRanges sr = new();

            foreach (GraphIndex.SourceRange r in ranges)
            {
                sr.Add(r);
            }

            Assert.AreEqual(ranges.Count, sr.Count);

            foreach (GraphIndex.SourceRange r in ranges)
            {
                AssertContained(sr, r);
            }
        }

        private static void AssertContained(SortedRanges sr, GraphIndex.SourceRange expected)
        {
            Assert.IsFalse(sr.TryGetValue(expected.Range.StartLine - 1, out GraphIndex.SourceRange _));
            {
                for (int l = expected.Range.StartLine; l < expected.Range.EndLine; l++)
                {
                    Assert.IsTrue(sr.TryGetValue(l, out GraphIndex.SourceRange actual));
                    Assert.AreSame(expected, actual);
                }
            }
            Assert.IsFalse(sr.TryGetValue(expected.Range.EndLine, out GraphIndex.SourceRange _));
        }

        [Test]
        public void TestOverlapTwice1()
        {
            PermuteOverlap(new GraphIndex.SourceRange[] { new(2, 7, new Node()), new(7, 7, new Node()) });
        }

        [Test]
        public void TestOverlapTwice2()
        {
            PermuteOverlap(new GraphIndex.SourceRange[] { new(1, 1, new Node()), new(1, 1, new Node()) });
        }

        [Test]
        public void TestOverlapThrice()
        {
            PermuteOverlap(new GraphIndex.SourceRange[] { new(5, 10, new Node()), new(1, 1, new Node()), new(7, 8, new Node()) });
        }

        [Test]
        public void TestOverlapFource()
        {
            PermuteOverlap(new GraphIndex.SourceRange[] { new(5, 10, new Node()), new(10, 15, new Node()),
                                         new(16, 20, new Node()), new(8, 12, new Node()) });
        }

        private static void PermuteOverlap(GraphIndex.SourceRange[] ranges)
        {
            foreach (IList<GraphIndex.SourceRange> permut in ranges.Permutations())
            {
                AssertOverlap(permut);
            }
        }

        private static void AssertOverlap(IList<GraphIndex.SourceRange> ranges)
        {
            SortedRanges sr = new();

            Assert.Throws<ArgumentException>(() =>
            {
                foreach (GraphIndex.SourceRange r in ranges)
                {
                    sr.Add(r);
                }
            });
        }
    }
}
