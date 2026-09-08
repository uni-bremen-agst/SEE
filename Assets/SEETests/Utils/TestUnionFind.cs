using NUnit.Framework;

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
                Assert.That(uf.Find(el), Is.EqualTo(el),
                            $"{el} must initially be its own representative.");
            }
            // Union elements with the same length
            uf.PartitionByValue();

            // Now a, b, c, d, e should be in the same set
            string root = uf.Find("a");
            Assert.That(uf.Find("b"), Is.EqualTo(root), "b must be in the same set as a.");
            Assert.That(uf.Find("c"), Is.EqualTo(root), "c must be in the same set as a.");
            Assert.That(uf.Find("d"), Is.EqualTo(root), "d must be in the same set as a.");
            Assert.That(uf.Find("e"), Is.EqualTo(root), "e must be in the same set as a.");
            Assert.That(uf.Find("ee"), Is.EqualTo(uf.Find("ff")),
                        "ee must be in the same set as ff.");

            // Strings with different lengths should be in different sets.
            Assert.That(uf.Find("ee"), Is.Not.EqualTo(uf.Find("ggg")),
                        "ee and ggg must be in different sets.");
            Assert.That(uf.Find("a"), Is.Not.EqualTo(uf.Find("ggg")),
                        "a and ggg must be in different sets.");
            Assert.That(uf.Find("a"), Is.Not.EqualTo(uf.Find("ff")),
                        "a and ff must be in different sets.");
        }

        [Test]
        public void TestUnionFindSingleElement()
        {
            // Arrange
            string[] elements = new[] { "a", "b" };
            UnionFind<string, int> uf = new(elements, s => s.Length);
            // "c" was not part of the initial set.
            Assert.That(() => uf.Union("a", "c"), Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void TestFind()
        {
            UnionFind<int, int> unionFind = new(new[] { 1, 2, 3 }, i => i);
            unionFind.Union(1, 2);

            int root = unionFind.Find(1);
            Assert.That(unionFind.Find(2), Is.EqualTo(root), "2 must be in the same set as 1.");
            Assert.That(unionFind.Find(3), Is.Not.EqualTo(root), "3 must be in a different set than 1.");
        }
    }
}
