using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Tests for <see cref="SourceRangeIndex"/>.
    /// </summary>
    internal class TestSourceRangeIndex : TestGraphBase
    {
        [Test]
        public void TestConsistentGraph()
        {
            SourceRangeIndex index = new(g);
            Assert.IsTrue(index.IsConsistent());
        }

        [Test]
        public void TestNonHomormorphicGraph()
        {
            // This node is logically in c1m1, but spatially nested in c1m1M3
            Node c1m1 = g.GetNode("c1.m1");
            Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            SourceRangeIndex index = new(g);
            Assert.IsFalse(index.IsConsistent());
        }

        [Test]
        public void TestOverlappingGraph()
        {
            // This node overlaps with c1m1M3
            //HIER WEITER
            Node c1m1 = g.GetNode("c1.m1");
            Child(g, c1m1, "c1.m1.M4", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            SourceRangeIndex index = new(g);
            Assert.IsFalse(index.IsConsistent());
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

        [SetUp]
        public void SetUp()
        {
            g = NewGraph();
            c1 = NewNode(g, "c1", type: "Class", directory: "mydir/", filename: "myfile.java", line: 50, length: 20);

            c1m1 = Child(g, c1, "c1.m1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 52, length: 5);
            c1m1M1 = Child(g, c1m1, "c1.m1.M1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 52, length: 1);
            c1m1M2 = Child(g, c1m1, "c1.m1.M2", type: "Method", directory: "mydir/", filename: "myfile.java", line: 53, length: 1);
            c1m1M3 = Child(g, c1m1, "c1.m1.M3", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            // This node is logically and spatially nested in c1m1M3
            c1m1M3M1 = Child(g, c1m1M3, "c1.m1.M3.M1", type: "Method", directory: "mydir/", filename: "myfile.java", line: 56, length: 1);

            c1m2 = Child(g, c1, "c1.m2", type: "Method", directory: "mydir/", filename: "myfile.java", line: 60, length: 5);
            // No filename => will be ignored.
            c1m3 = Child(g, c1, "c1.m3", type: "Method");
            // No filename => will be ignored.
            c1m4 = Child(g, c1, "c1.m4", type: "Method", directory: "mydir/", line: 60, length: 5);

            // In a new file, because the directory is missing.
            c1m5 = Child(g, c1, "c1.m5", type: "Method", filename: "myfile.java", line: 66, length: null);
            // No source line => will be ignored.
            c1m6 = Child(g, c1, "c1.m6", type: "Method", filename: "myfile.java", line: null, length: 5);
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