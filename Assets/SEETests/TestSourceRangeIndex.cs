using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// Tests for <see cref="SourceRangeIndex"/>.
    /// </summary>
    internal class TestSourceRangeIndex : TestGraphBase
    {
        private SourceRangeIndex GetIndexByPath(Graph graph)
        {
            return new(graph, node => node.Path());
        }

        [Test]
        public void TestConsistent1()
        {
            SourceRangeIndex index = GetIndexByPath(g);
            Assert.IsTrue(index.IsIsomorphic());
            // 11 nodes were added to the graph, but 2 are ignored because of an
            // empty path.
            Assert.AreEqual(9, index.Count);

            AssertNotFound(index, c1, 1);
            AssertNotFound(index, c1, 49);
            AssertFound(index, c1, 50);
            AssertFound(index, c1, 51);
            AssertNotFound(index, c1, 52);
            AssertNotFound(index, c1, 54);
            AssertFound(index, c1, 69);
            AssertNotFound(index, c1, 70);

            AssertNotFound(index, c1m1, 51);
            AssertNotFound(index, c1m1, 52);
            AssertNotFound(index, c1m1, 53);
            AssertFound(index, c1m1, 54);
            AssertFound(index, c1m1, 55);
            AssertNotFound(index, c1m1, 56);
            AssertFound(index, c1m1, 57);
            AssertNotFound(index, c1m1, 58);

            AssertNotFound(index, c1m1M1, 51);
            AssertFound(index, c1m1M1, 52);
            AssertNotFound(index, c1m1M1, 53);

            AssertNotFound(index, c1m1M2, 52);
            AssertFound(index, c1m1M2, 53);
            AssertNotFound(index, c1m1M2, 54);

            AssertNotFound(index, c1m1M3, 55);
            AssertNotFound(index, c1m1M3, 56);
            AssertNotFound(index, c1m1M3, 57);

            AssertNotFound(index, c1m1M3M1, 55);
            AssertFound(index, c1m1M3M1, 56);
            AssertNotFound(index, c1m1M3M1, 57);

            AssertNotFound(index, c1m2, 59);
            AssertFound(index, c1m2, 60);
            AssertFound(index, c1m2, 61);
            AssertFound(index, c1m2, 62);
            AssertFound(index, c1m2, 63);
            AssertFound(index, c1m2, 64);
            AssertNotFound(index, c1m2, 65);

            AssertNotFound(index, c1m5, 65);
            AssertFound(index, c1m5, 66);
            AssertNotFound(index, c1m5, 67);

            AssertNotFound(index, c1m4, 59);
            AssertFound(index, c1m4, 60);
            AssertFound(index, c1m4, 64);
            AssertNotFound(index, c1m4, 65);
        }

        [Test]
        public void TestConsistent2()
        {
            // This node should fit.
            Node c1m1M4 = Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 54, length: 1);

            SourceRangeIndex index = GetIndexByPath(g);
            Assert.IsTrue(index.IsIsomorphic());
            AssertFound(index, c1m1M4, 54);
        }

        /// <summary>
        /// Asserts that either no node can be found in the file containing <paramref name="unexpected"/>
        /// at the given <paramref name="line"/> or -- if one is found -- that the found node is
        /// different from <paramref name="unexpected"/>.
        /// </summary>
        /// <param name="index">where to search</param>
        /// <param name="unexpected">node not expected to be found</param>
        /// <param name="line">source line where to search</param>
        private static void AssertNotFound(SourceRangeIndex index, Node unexpected, int line)
        {
            Assert.IsTrue(!index.TryGetValue(unexpected.Path(), line, out Node node) // no node found at all
                || (node != null && unexpected != node)); // if one is found, it must be different from unexpected
        }

        private static void AssertFound(SourceRangeIndex index, Node expected, int line)
        {
            Assert.IsTrue(index.TryGetValue(expected.Path(), line, out Node found));
            Assert.AreEqual(expected, found);
        }

        [Test]
        public void TestNonHomormorphicGraph()
        {
            // This node is logically in c1m1, but spatially nested in c1m1M3
            Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            SourceRangeIndex index = GetIndexByPath(g);
            Assert.IsFalse(index.IsIsomorphic());

            LogAssert.Expect(LogType.Error, new Regex(@"Range c1.m1.M4.* is subsumed by c1.m1.M3.M1.*"));
        }

        [Test]
        public void TestSubsumption()
        {
            // This node is subsumed by c1.m1.M2, but is not in parent-child relation in the node hierarchy
            Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 53, length: 1);

            SourceRangeIndex index = GetIndexByPath(g);
            Assert.IsFalse(index.IsIsomorphic());

            LogAssert.Expect(LogType.Error, new Regex("Range .* is subsumed by .*"));
        }

        [Test]
        public void TestOverlapping()
        {
            // This node overlaps with c1.m1.M3 but is not subsumed only by c1.m1. This node and
            // c1.m1.M3 are siblings in the node hierarchy and they would also be siblings in the
            // source-range hierarchy of the index; yet they overlap. Hence, an exception will be
            // thrown.
            Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 54, length: 3);

            Assert.Throws<ArgumentException>(() =>  new SourceRangeIndex(g, node => node.Path()));
        }

        private Graph g;
        private Node c1;
        private Node c1m1;
        private Node c1m1M1;
        private Node c1m1M2;
        private Node c1m1M3;
        private Node c1m1M3M1;
        private Node c1m2;
        private Node c1m3;
        private Node c1m4;
        private Node c1m5;
        private Node c1m6;

        /// <summary>
        /// Generates a graph whose source-range index should look like so:
        ///
        /// *** mydir/myfile.java ***
        /// 1       c1@[50, 69]
        /// 1.1       c1.m1@[52, 57]
        /// 1.1.1       c1.m1.M1@[52, 52]
        /// 1.1.2       c1.m1.M2@[53, 53]
        /// 1.1.3       c1.m1.M3@[56, 56]
        /// 1.1.3.1       c1.m1.M3.M1@[56, 56]
        /// 1.2       c1.m2@[60, 64]
        ///
        /// *** mydir/ ***
        /// 1 c1.m4 @mydir/:60-64
        ///
        /// *** myfile.java ***
        /// 1 c1.m5@[66, 66]
        ///
        /// c1.m3 does not have a path. Will be ignored.
        /// c1.m6 does not have a source line. Will be ignored.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            g = NewEmptyGraph();
            c1 = NewNode(g, "c1", type: "Class", directory: "mydir/", filename: "myfile.java", line: 50, length: 20);

            c1m1 = Child(g, c1, "c1.m1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 52, length: 6);
            c1m1M1 = Child(g, c1m1, "c1.m1.M1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 52, length: 1);
            c1m1M2 = Child(g, c1m1, "c1.m1.M2", type: "Method", directory: "mydir/", filename: "myfile.java", line: 53, length: 1);
            c1m1M3 = Child(g, c1m1, "c1.m1.M3", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            // This node is logically and spatially nested in c1m1M3
            c1m1M3M1 = Child(g, c1m1M3, "c1.m1.M3.M1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            c1m2 = Child(g, c1, "c1.m2", type: "Method", directory: "mydir/", filename: "myfile.java", line: 60, length: 5);
            // Empty path => will be ignored.
            c1m3 = Child(g, c1, "c1.m3", type: "Method");
            // Path consists of only a directory without filename => will still be added to the index, because path is not empty.
            c1m4 = Child(g, c1, "c1.m4", type: "Method", directory: "mydir/", line: 60, length: 5);

            // In a new file, because the directory is missing.
            c1m5 = Child(g, c1, "c1.m5", type: "Method", filename: "myfile.java", line: 66, length: null);
            // No source line => will be ignored.
            c1m6 = Child(g, c1, "c1.m6", type: "Method", filename: "myfile.java", line: null, length: 5);
        }

        [Test]
        public void SameRangeTwice()
        {
            g = NewEmptyGraph();
            NewNode(g, "c1", type: "Class", directory: "mydir/", filename: "myfile.java", line: 50, length: 20);
            NewNode(g, "c2", type: "Class", directory: "mydir/", filename: "myfile.java", line: 50, length: 20);
            NewNode(g, "c3", type: "Class", directory: "mydir/", filename: "myfile.java", line: 50, length: 30);

            SourceRangeIndex index = GetIndexByPath(g);
            Assert.IsFalse(index.IsIsomorphic());

            LogAssert.Expect(LogType.Error, new Regex(@"Range c2.* is subsumed by c1.*"));
            LogAssert.Expect(LogType.Error, new Regex(@"Range c3.* is subsumed by c2.*"));
        }

        [TearDown]
        public void TearDown()
        {
            c1 = null;
            c1m1 = null;
            c1m1M1 = null;
            c1m1M2 = null;
            c1m1M3 = null;
            c1m1M3M1 = null;
            c1m2 = null;
            c1m3 = null;
            c1m4 = null;
            c1m5 = null;
            c1m6 = null;
            g = null;
        }
    }
}