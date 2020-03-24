using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.GO;

namespace SEE.Layout.RectanglePacking
{
    /// <summary>
    /// Unit tests for RectanglePacker.
    /// </summary>
    internal class TestRectanglePacker
    {
        /// <summary>
        /// True if left and right are the same list (order is ignored).
        /// </summary>
        /// <param name="left">left list</param>
        /// <param name="right">right list</param>
        /// <returns>left and right have the very same elements</returns>
        private static bool EqualLists(IList<PNode> left, IList<PNode> right)
        {
            // Note: the following condition does not deal with duplicates.
            bool result = left.All(right.Contains) && left.Count == right.Count;
            if (!result)
            {
                foreach (PNode node in left)
                {
                    if (!right.Contains(node))
                    {
                        Debug.LogErrorFormat("{0} contained in left, but not in right list.\n", node.ToString());
                    }
                }
                foreach (PNode node in right)
                {
                    if (!left.Contains(node))
                    {
                        Debug.LogErrorFormat("{0} contained in right, but not in left list.\n", node.ToString());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Runs the example scenario used by Richard Wettel in his dissertation 
        /// plus two additions at the end to check situations he did not cover
        /// in this example. See page 36 in "Software Systems as Cities" by
        /// Richard Wettel.
        /// </summary>
        [Test]
        public void TestSplit()
        {
            Debug.Log("TestSplit\n");

            Vector2 totalSize = new Vector2(14, 12);
            PTree tree = new PTree(Vector2.zero, totalSize);

            PNode A = tree.Root;
            Assert.That(A.occupied, Is.False);
            Assert.That(A.rectangle.position, Is.EqualTo(Vector2.zero));
            Assert.That(A.rectangle.size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new Vector2(8, 6);
            PNode result = tree.Split(A, EL1size);

            PNode B = A.left;
            PNode C = A.right;
            PNode El1 = B.left;
            PNode D = B.right;

            Assert.AreSame(result, El1);

            Assert.That(A.occupied, Is.False);
            Assert.That(A.rectangle.position, Is.EqualTo(Vector2.zero));
            Assert.That(A.rectangle.size, Is.EqualTo(totalSize));

            Assert.That(B.occupied, Is.False);
            Assert.That(B.rectangle.position, Is.EqualTo(Vector2.zero));
            Assert.That(B.rectangle.size, Is.EqualTo(new Vector2(14, 6)));

            Assert.That(El1.occupied, Is.True);
            Assert.That(El1.rectangle.position, Is.EqualTo(Vector2.zero));
            Assert.That(El1.rectangle.size, Is.EqualTo(EL1size));

            Assert.That(C.occupied, Is.False);
            Assert.That(C.rectangle.position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(C.rectangle.size, Is.EqualTo(new Vector2(14, 6)));

            Assert.That(D.occupied, Is.False);
            Assert.That(D.rectangle.position, Is.EqualTo(new Vector2(8, 0)));
            Assert.That(D.rectangle.size, Is.EqualTo(new Vector2(6, 6)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Second split
            result = tree.Split(C, new Vector2(7, 3));
            PNode E = C.left;
            PNode F = C.right;
            PNode El2 = E.left;
            PNode G = E.right;

            Assert.AreSame(result, El2);

            Assert.That(El2.occupied, Is.True);
            Assert.That(El2.rectangle.position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(El2.rectangle.size, Is.EqualTo(new Vector2(7, 3)));

            Assert.That(G.occupied, Is.False);
            Assert.That(G.rectangle.position, Is.EqualTo(new Vector2(7, 6)));
            Assert.That(G.rectangle.size, Is.EqualTo(new Vector2(7, 3)));

            Assert.That(E.occupied, Is.False);
            Assert.That(E.rectangle.position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(E.rectangle.size, Is.EqualTo(new Vector2(14, 3)));

            Assert.That(F.occupied, Is.False);
            Assert.That(F.rectangle.position, Is.EqualTo(new Vector2(0, 9)));
            Assert.That(F.rectangle.size, Is.EqualTo(new Vector2(14, 3)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { D, G, F }), Is.True);

            // Third split
            // requested rectangle has same height as G
            result = tree.Split(G, new Vector2(5, G.rectangle.size.y));
            PNode El3 = G.left;
            PNode H = G.right;

            Assert.AreSame(result, El3);

            Assert.That(El3.occupied, Is.True);
            Assert.That(El3.rectangle.position, Is.EqualTo(G.rectangle.position));
            Assert.That(El3.rectangle.size, Is.EqualTo(new Vector2(5, 3)));

            Assert.That(H.occupied, Is.False);
            Assert.That(H.rectangle.position, Is.EqualTo(G.rectangle.position + new Vector2(5, 0)));
            Assert.That(H.rectangle.size, Is.EqualTo(new Vector2(2, 3)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { D, H, F }), Is.True);

            // Fourth split
            result = tree.Split(D, new Vector2(4, 4));
            PNode I = D.left;
            PNode J = D.right;
            PNode El4 = I.left;
            PNode K = I.right;

            Assert.AreSame(result, El4);

            Assert.That(El4.occupied, Is.True);
            Assert.That(El4.rectangle.position, Is.EqualTo(D.rectangle.position));
            Assert.That(El4.rectangle.size, Is.EqualTo(new Vector2(4, 4)));

            Assert.That(I.occupied, Is.False);
            Assert.That(I.rectangle.position, Is.EqualTo(D.rectangle.position));
            Assert.That(I.rectangle.size, Is.EqualTo(new Vector2(D.rectangle.size.x, El4.rectangle.size.y)));

            Assert.That(J.occupied, Is.False);
            Assert.That(J.rectangle.position, Is.EqualTo(D.rectangle.position + new Vector2(0, El4.rectangle.size.y)));
            Assert.That(J.rectangle.size, Is.EqualTo(new Vector2(D.rectangle.size.x, D.rectangle.size.y - El4.rectangle.size.y)));

            Assert.That(K.occupied, Is.False);
            Assert.That(K.rectangle.position, Is.EqualTo(D.rectangle.position + new Vector2(El4.rectangle.size.x, 0)));
            Assert.That(K.rectangle.size, Is.EqualTo(new Vector2(D.rectangle.size.x - El4.rectangle.size.x, El4.rectangle.size.y)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { J, K, H, F }), Is.True);

            // Fifth split
            // perfect match
            result = tree.Split(J, J.rectangle.size);

            Assert.AreSame(result, J);

            Assert.That(J.occupied, Is.True);
            Assert.That(J.left, Is.Null);
            Assert.That(J.right, Is.Null);

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, F }), Is.True);

            // Sixth split
            // requested rectangle has same width as F
            result = tree.Split(F, new Vector2(F.rectangle.size.x, 1));
            PNode Fleft = F.left;
            PNode Fright = F.right;

            Assert.AreSame(result, Fleft);

            Assert.That(Fleft.occupied, Is.True);
            Assert.That(Fleft.rectangle.position, Is.EqualTo(F.rectangle.position));
            Assert.That(Fleft.rectangle.size, Is.EqualTo(new Vector2(F.rectangle.size.x, 1)));

            Assert.That(Fright.occupied, Is.False);
            Assert.That(Fright.rectangle.position, Is.EqualTo(F.rectangle.position + new Vector2(0, Fleft.rectangle.size.y)));
            Assert.That(Fright.rectangle.size, Is.EqualTo(new Vector2(F.rectangle.size.x, F.rectangle.size.y - Fleft.rectangle.size.y)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, Fright }), Is.True);

            // Debug.Log(A.ToString() + "\n");
        }

        /// <summary>
        /// Let's us explore performance issues. Just adjust parameter howManyNodes.
        /// </summary>
        [Test]
        public void TestLayout()
        {
            int howManyNodes = 500;
            Vector3 initialSize = Vector3.one;

            CubeFactory factory = new CubeFactory();
            ICollection<ILayoutNode> gameObjects = new List<ILayoutNode>();

            for (int i = 1; i <= howManyNodes; i++)
            {
                initialSize *= 1.01f;
                gameObjects.Add(new MyGameNode(initialSize, i));
            }

            RectanglePacker packer = new RectanglePacker(0.0f, 1.0f);

            Dictionary<ILayoutNode, NodeTransform> layout = packer.Layout(gameObjects);
        }

        private class MyGameNode : ILayoutNode
        {
            private readonly int index;

            public MyGameNode(Vector3 initialSize, int index)
            {
                this.scale = initialSize;
                this.index = index;
            }

            public ILayoutNode Parent => null;

            private int level = 0;

            public int Level
            {
                get => level;
                set => level = value;
            }

            public IList<ILayoutNode> Children()
            {
                return new List<ILayoutNode>();
            }

            private Vector3 scale;

            public Vector3 Scale
            {
                get => scale;
                set => scale = value;
            }

            private Vector3 centerPosition;

            public Vector3 CenterPosition
            {
                get => centerPosition;
                set => centerPosition = value;
            }

            public Vector3 Roof
            {
                get => centerPosition + Vector3.up * 0.5f * scale.y;
            }

            public Vector3 Ground
            {
                get => centerPosition - Vector3.up * 0.5f * scale.y;
            }

            public bool IsLeaf => false;

            public string LinkName { get => index.ToString(); }

            private float rotation;

            public float Rotation
            {
                get => rotation;
                set => rotation = value;
            }

            public ICollection<ILayoutNode> Successors => new List<ILayoutNode>();
        }
    }
}

