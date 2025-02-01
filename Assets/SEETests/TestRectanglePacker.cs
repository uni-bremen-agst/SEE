using NUnit.Framework;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            Vector2 totalSize = new Vector2(14, 12);
            PTree tree = new(Vector2.zero, totalSize);

            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new Vector2(8, 6);
            PNode result = tree.Split(A, EL1size);

            PNode B = A.Left;
            PNode C = A.Right;
            PNode El1 = B.Left;
            PNode D = B.Right;

            Assert.AreSame(result, El1);

            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            Assert.That(B.Occupied, Is.False);
            Assert.That(B.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(B.Rectangle.Size, Is.EqualTo(new Vector2(14, 6)));

            Assert.That(El1.Occupied, Is.True);
            Assert.That(El1.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(El1.Rectangle.Size, Is.EqualTo(EL1size));

            Assert.That(C.Occupied, Is.False);
            Assert.That(C.Rectangle.Position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(C.Rectangle.Size, Is.EqualTo(new Vector2(14, 6)));

            Assert.That(D.Occupied, Is.False);
            Assert.That(D.Rectangle.Position, Is.EqualTo(new Vector2(8, 0)));
            Assert.That(D.Rectangle.Size, Is.EqualTo(new Vector2(6, 6)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Second split
            result = tree.Split(C, new Vector2(7, 3));
            PNode E = C.Left;
            PNode F = C.Right;
            PNode El2 = E.Left;
            PNode G = E.Right;

            Assert.AreSame(result, El2);

            Assert.That(El2.Occupied, Is.True);
            Assert.That(El2.Rectangle.Position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(El2.Rectangle.Size, Is.EqualTo(new Vector2(7, 3)));

            Assert.That(G.Occupied, Is.False);
            Assert.That(G.Rectangle.Position, Is.EqualTo(new Vector2(7, 6)));
            Assert.That(G.Rectangle.Size, Is.EqualTo(new Vector2(7, 3)));

            Assert.That(E.Occupied, Is.False);
            Assert.That(E.Rectangle.Position, Is.EqualTo(new Vector2(0, 6)));
            Assert.That(E.Rectangle.Size, Is.EqualTo(new Vector2(14, 3)));

            Assert.That(F.Occupied, Is.False);
            Assert.That(F.Rectangle.Position, Is.EqualTo(new Vector2(0, 9)));
            Assert.That(F.Rectangle.Size, Is.EqualTo(new Vector2(14, 3)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { D, G, F }), Is.True);

            // Third split
            // requested rectangle has same height as G
            result = tree.Split(G, new Vector2(5, G.Rectangle.Size.y));
            PNode El3 = G.Left;
            PNode H = G.Right;

            Assert.AreSame(result, El3);

            Assert.That(El3.Occupied, Is.True);
            Assert.That(El3.Rectangle.Position, Is.EqualTo(G.Rectangle.Position));
            Assert.That(El3.Rectangle.Size, Is.EqualTo(new Vector2(5, 3)));

            Assert.That(H.Occupied, Is.False);
            Assert.That(H.Rectangle.Position, Is.EqualTo(G.Rectangle.Position + new Vector2(5, 0)));
            Assert.That(H.Rectangle.Size, Is.EqualTo(new Vector2(2, 3)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { D, H, F }), Is.True);

            // Fourth split
            result = tree.Split(D, new Vector2(4, 4));
            PNode I = D.Left;
            PNode J = D.Right;
            PNode El4 = I.Left;
            PNode K = I.Right;

            Assert.AreSame(result, El4);

            Assert.That(El4.Occupied, Is.True);
            Assert.That(El4.Rectangle.Position, Is.EqualTo(D.Rectangle.Position));
            Assert.That(El4.Rectangle.Size, Is.EqualTo(new Vector2(4, 4)));

            Assert.That(I.Occupied, Is.False);
            Assert.That(I.Rectangle.Position, Is.EqualTo(D.Rectangle.Position));
            Assert.That(I.Rectangle.Size, Is.EqualTo(new Vector2(D.Rectangle.Size.x, El4.Rectangle.Size.y)));

            Assert.That(J.Occupied, Is.False);
            Assert.That(J.Rectangle.Position, Is.EqualTo(D.Rectangle.Position + new Vector2(0, El4.Rectangle.Size.y)));
            Assert.That(J.Rectangle.Size, Is.EqualTo(new Vector2(D.Rectangle.Size.x, D.Rectangle.Size.y - El4.Rectangle.Size.y)));

            Assert.That(K.Occupied, Is.False);
            Assert.That(K.Rectangle.Position, Is.EqualTo(D.Rectangle.Position + new Vector2(El4.Rectangle.Size.x, 0)));
            Assert.That(K.Rectangle.Size, Is.EqualTo(new Vector2(D.Rectangle.Size.x - El4.Rectangle.Size.x, El4.Rectangle.Size.y)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { J, K, H, F }), Is.True);

            // Fifth split
            // perfect match
            result = tree.Split(J, J.Rectangle.Size);

            Assert.AreSame(result, J);

            Assert.That(J.Occupied, Is.True);
            Assert.That(J.Left, Is.Null);
            Assert.That(J.Right, Is.Null);

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, F }), Is.True);

            // Sixth split
            // requested rectangle has same width as F
            result = tree.Split(F, new Vector2(F.Rectangle.Size.x, 1));
            PNode Fleft = F.Left;
            PNode Fright = F.Right;

            Assert.AreSame(result, Fleft);

            Assert.That(Fleft.Occupied, Is.True);
            Assert.That(Fleft.Rectangle.Position, Is.EqualTo(F.Rectangle.Position));
            Assert.That(Fleft.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, 1)));

            Assert.That(Fright.Occupied, Is.False);
            Assert.That(Fright.Rectangle.Position, Is.EqualTo(F.Rectangle.Position + new Vector2(0, Fleft.Rectangle.Size.y)));
            Assert.That(Fright.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, F.Rectangle.Size.y - Fleft.Rectangle.Size.y)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, Fright }), Is.True);

            // Debug.Log(A.ToString() + "\n");
        }

        /// <summary>
        /// Let's us explore performance issues.
        /// </summary>
        [Test]
        public void TestLayout()
        {
            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes();

            RectanglePackingNodeLayout packer = new RectanglePackingNodeLayout(0.01f);

            Dictionary<ILayoutNode, NodeTransform> layout = packer.Layout(gameObjects, Vector2.one);
        }
    }
}

