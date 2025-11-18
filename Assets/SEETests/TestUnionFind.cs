using NUnit.Framework;
using UnityEngine.TestTools;

namespace SEE.Utils
{
    /// <summary>
    /// Tests for <see cref="UnionFind{O, V}"/>.
    /// </summary>
    internal class TestUnionFind
    {
        [Test]
        public void TestUnionFindSimple()
        {
            // Arrange
            string[] elements = new[] { "a", "b", "c", "d", "e", "ff", "ggg", "ee" };
            // Group by string length.
            UnionFind<string, int> uf = new(elements, s => s.Length);

            // Initially, each element is its own parent
            foreach (string el in elements)
            {
                Assert.AreEqual(el, uf.Find(el));
            }
            // Union elements with the same length
            uf.PartitionByValue();

            // Now a, b, c, d, e should be in the same set
            string root = uf.Find("a");
            Assert.AreEqual(root, uf.Find("b"));
            Assert.AreEqual(root, uf.Find("c"));
            Assert.AreEqual(root, uf.Find("d"));
            Assert.AreEqual(root, uf.Find("e"));
            Assert.AreEqual(uf.Find("ff"), uf.Find("ee"));

            // Strings with different lengths should be in different sets.
            Assert.AreNotEqual(uf.Find("ggg"), uf.Find("ee"));
            Assert.AreNotEqual(uf.Find("ggg"), uf.Find("a"));
            Assert.AreNotEqual(uf.Find("ff"), uf.Find("a"));
        }

        [Test]
        public void TestUnionFindSingleElement()
        {
            // Arrange
            string[] elements = new[] { "a", "b" };
            UnionFind<string, int> uf = new(elements, s => s.Length);
            // "c" was not part of the initial set.
            Assert.Throws<System.ArgumentException>(() => uf.Union("a", "c"));
        }
    }
}
