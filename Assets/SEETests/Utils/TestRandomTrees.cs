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
            Assert.That(parent, Has.Length.EqualTo(n));
            Assert.That(root, Is.EqualTo(-1));
        }

        [Test]
        public void TestOne()
        {
            // tree with single node requested
            int n = 1;
            int[] parent = RandomTrees.Random(n, out int root);
            Assert.That(parent, Has.Length.EqualTo(n));
            Assert.That(parent[0], Is.EqualTo(-1));
            Assert.That(root, Is.EqualTo(0));
        }

        private void AssertTree(int[] parent, int root)
        {
            // All entries of parent must be in range [-1, parent.Length-1];
            // an entry is -1 only for the root
            for (int i = 0; i < parent.Length; i++)
            {
                int node = parent[i];
                Assert.That(node, Is.InRange(-1, parent.Length - 1));
                if (node == -1)
                {
                    Assert.That(i, Is.EqualTo(root), "Only the root may have -1 as its parent.");
                }
            }
            // default for bool in C# is false
            bool[] visited = new bool[parent.Length];
            Visit(root, parent, visited);
            // Make sure every node was visited.
            Assert.That(visited, Is.All.True);
        }

        private void Visit(int node, int[] parent, bool[] visited)
        {
            Assert.That(visited[node], Is.False, $"Node {node} was visited more than once.");
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
                Assert.That(parent, Has.Length.EqualTo(n));
                Assert.That(parent[root], Is.EqualTo(-1));
                AssertTree(parent, root);
            }
        }
    }
}