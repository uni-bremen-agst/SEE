using NUnit.Framework;
using SEE.Layout.Utils;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Unit tests for LCAFinder.
    /// </summary>
    internal class TestLCAFinder
    {
        private int nodeID = 0;

        private LNode NewVertex(string name = "")
        {
            LNode node = new LNode();
            if (string.IsNullOrEmpty(name))
            {
                node.LinkName = nodeID.ToString();
                nodeID++;
            }
            else
            {
                node.LinkName = name;
            }
            return node;
        }

        private class LNode : IHierarchyNode<LNode>
        {
            public LNode Parent { private set; get; }


            private int level;

            public int Level
            {
                get => level;
                set => level = value;
            }

            public bool IsLeaf => children.Count == 0;

            private string linkname;

            public string LinkName
            {
                get => linkname;
                set => linkname = value;
            }

            private readonly ICollection<LNode> children = new List<LNode>();

            public ICollection<LNode> Children()
            {
                return children;
            }

            public void AddChild(LNode child)
            {
                child.Parent = this;
                children.Add(child);
            }

            public void RemoveChild(LNode child)
            {
                children.Remove(child);
                child.Parent = null;
            }
        }

        [SetUp]
        protected void Setup()
        {
            nodeID = 0;
        }

        [Test]
        public void TestEmpty()
        {
            Assert.That(() => new LCAFinder<LNode>((LNode)null), Throws.ArgumentNullException);
        }

        [Test]
        public void TestSingle()
        {
            LNode root = NewVertex();
            LCAFinder<LNode> lca = new LCAFinder<LNode>(root);

            Assert.That(lca.LCA(root, root), Is.EqualTo(root));
        }

        [Test]
        public void TestSimple()
        {
            //       root
            //       /  \
            //      a    b
            LNode root = NewVertex();
            LNode a = NewVertex();
            LNode b = NewVertex();
            root.AddChild(a);
            root.AddChild(b);

            LCAFinder<LNode> lca = new LCAFinder<LNode>(root);

            Assert.That(lca.LCA(a, b), Is.EqualTo(root));
            Assert.That(lca.LCA(root, b), Is.EqualTo(root));
            Assert.That(lca.LCA(b, root), Is.EqualTo(root));
            Assert.That(lca.LCA(a, root), Is.EqualTo(root));
            Assert.That(lca.LCA(root, a), Is.EqualTo(root));
        }

        [Test]
        public void TestChain()
        {
            //       root
            //        |
            //        a
            //        |
            //        b
            LNode root = NewVertex();
            LNode a = NewVertex();
            LNode b = NewVertex();
            root.AddChild(a);
            a.AddChild(b);

            LCAFinder<LNode> lca = new LCAFinder<LNode>(root);

            Assert.That(lca.LCA(root, root), Is.EqualTo(root));

            Assert.That(lca.LCA(a, b), Is.EqualTo(a));
            Assert.That(lca.LCA(b, a), Is.EqualTo(a));

            Assert.That(lca.LCA(a, root), Is.EqualTo(root));
            Assert.That(lca.LCA(root, a), Is.EqualTo(root));

            Assert.That(lca.LCA(b, root), Is.EqualTo(root));
            Assert.That(lca.LCA(root, b), Is.EqualTo(root));
        }

        [Test]
        public void TestMultiLevel()
        {
            //        root
            //       / |   \
            //      a  b    c
            //     /\  |    /\
            //   a1 a2 b1 c1 c2
            //            /\
            //          c11 c12

            LNode root = NewVertex("root");
            LNode a = NewVertex("a");
            LNode b = NewVertex("b");
            LNode c = NewVertex("c");
            LNode a1 = NewVertex("a1");
            LNode a2 = NewVertex("a2");
            LNode b1 = NewVertex("b1");
            LNode c1 = NewVertex("c1");
            LNode c2 = NewVertex("c2");
            LNode c11 = NewVertex("c11");
            LNode c12 = NewVertex("c12");

            root.AddChild(a);
            root.AddChild(b);
            root.AddChild(c);

            a.AddChild(a1);
            a.AddChild(a2);

            b.AddChild(b1);

            c.AddChild(c1);
            c.AddChild(c2);
            c1.AddChild(c11);
            c1.AddChild(c12);

            LCAFinder<LNode> lca = new LCAFinder<LNode>(root);

            Assert.That(lca.LCA(a1, a2), Is.EqualTo(a));
            Assert.That(lca.LCA(a2, b1), Is.EqualTo(root));
            Assert.That(lca.LCA(b1, c12), Is.EqualTo(root));
            Assert.That(lca.LCA(c11, c12), Is.EqualTo(c1));
            Assert.That(lca.LCA(c2, c12), Is.EqualTo(c));
            Assert.That(lca.LCA(c1, c), Is.EqualTo(c));
        }

        [Test]
        public void TestForrest()
        {
            //        r2  r2
            //       / |   \
            //      a  b    c
            //     /\  |    /\
            //   a1 a2 b1 c1 c2
            //            /\
            //          c11 c12

            LNode r1 = NewVertex("r1");
            LNode a = NewVertex("a");
            LNode b = NewVertex("b");
            LNode a1 = NewVertex("a1");
            LNode a2 = NewVertex("a2");
            LNode b1 = NewVertex("b1");
            r1.AddChild(a);
            r1.AddChild(b);
            a.AddChild(a1);
            a.AddChild(a2);
            b.AddChild(b1);

            LNode r2 = NewVertex("r2");
            LNode c = NewVertex("c");
            LNode c1 = NewVertex("c1");
            LNode c2 = NewVertex("c2");
            LNode c11 = NewVertex("c11");
            LNode c12 = NewVertex("c12");

            r2.AddChild(c);
            c.AddChild(c1);
            c.AddChild(c2);
            c1.AddChild(c11);
            c1.AddChild(c12);

            ICollection<LNode> roots = new List<LNode>();
            roots.Add(r1);
            roots.Add(r2);

            LCAFinder<LNode> lca = new LCAFinder<LNode>(roots);

            Assert.That(lca.LCA(a1, a2), Is.EqualTo(a));
            Assert.That(lca.LCA(a2, b1), Is.EqualTo(r1));

            Assert.That(lca.LCA(c11, c12), Is.EqualTo(c1));
            Assert.That(lca.LCA(c2, c12), Is.EqualTo(c));
            Assert.That(lca.LCA(c1, c), Is.EqualTo(c));
            Assert.That(lca.LCA(r2, c12), Is.EqualTo(r2));

            Assert.That(lca.LCA(b1, c12), Is.Null);
            Assert.That(lca.LCA(r1, r2), Is.Null);
        }
    }
}