using NUnit.Framework;

namespace SEE.Utils
{
    /// <summary>
    /// Test cases for RandomTrees.Random.
    /// </summary>
    public class TestRandomTrees
    {
        [Test]
        public void TestNegative()
        {
            // negative number of requested nodes
            Assert.That(() => RandomTrees.Random(-1, out int root), Throws.Exception);
        }

        [Test]
        public void TestEmpty()
        {
            // empty tree requested
            int n = 0;
            int[] parent = RandomTrees.Random(n, out int root);
            Assert.AreEqual(n, parent.Length);
            Assert.AreEqual(-1, root);
        }

        [Test]
        public void TestOne()
        {
            // tree with single node requested
            int n = 1;
            int[] parent = RandomTrees.Random(n, out int root);
            Assert.AreEqual(n, parent.Length);
            Assert.AreEqual(-1, parent[0]);
            Assert.AreEqual(0, root);
        }

        private void AssertTree(int[] parent, int root)
        {
            // All entries of parent must be in range [-1, parent.Length-1];
            // an entry is -1 only for the root
            for (int i = 0; i < parent.Length; i++)
            {
                int node = parent[i];
                Assert.That(node >= -1);
                Assert.That(node < parent.Length);
                if (node == -1)
                {
                    Assert.That(i == root);
                }
            }
            // default for bool in C# is false
            bool[] visited = new bool[parent.Length];
            Visit(root, parent, visited);
            // Make sure every node was visited.
            foreach (bool v in visited)
            {
                Assert.That(v);
            }
        }

        private void Visit(int node, int[] parent, bool[] visited)
        {
            Assert.That(!visited[node]);
            visited[node] = true;
            for (int i = 0; i < parent.Length; i++)
            {
                if (parent[i] == node)
                {
                    // i is a child of node
                    Visit(i, parent, visited);
                }
            }
        }

        [Test]
        public void TestMany()
        {
            // trees with more than one node requested
            for (int n = 2; n <= 100; n++)
            {
                int[] parent = RandomTrees.Random(n, out int root);
                Assert.AreEqual(n, parent.Length);
                Assert.AreEqual(-1, parent[root]);
                AssertTree(parent, root);
            }
        }
    }
}