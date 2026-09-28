using NUnit.Framework;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.RectanglePacking
{
    /// <summary>
    /// Unit tests for RectanglePacker.
    /// </summary>
    internal class TestRectanglePacker
    {
        /// <summary>
        /// Runs the example scenario used by Richard Wettel in his dissertation
        /// plus two additions at the end to check situations he did not cover
        /// in this example. See page 36 in "Software Systems as Cities" by
        /// Richard Wettel.
        /// </summary>
        [Test]
        public void TestSplit()
        {
            Vector2 totalSize = new(14, 12);
            PTree tree = new(Vector2.zero, totalSize);

            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(8, 6);
            PNode result = tree.Split(A, EL1size);

            PNode B = A.Left;
            PNode C = A.Right;
            PNode El1 = B.Left;
            PNode D = B.Right;

            Assert.That(result, Is.SameAs(El1), "First split must return the node newly occupied by El1.");

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

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { C, D }),
                        "Free leaves after the first split.");

            // Second split
            result = tree.Split(C, new Vector2(7, 3));
            PNode E = C.Left;
            PNode F = C.Right;
            PNode El2 = E.Left;
            PNode G = E.Right;

            Assert.That(result, Is.SameAs(El2), "Second split must return the node newly occupied by El2.");

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

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { D, G, F }),
                        "Free leaves after the second split.");

            // Third split
            // requested rectangle has same height as G
            result = tree.Split(G, new Vector2(5, G.Rectangle.Size.y));
            PNode El3 = G.Left;
            PNode H = G.Right;

            Assert.That(result, Is.SameAs(El3), "Third split must return the node newly occupied by El3.");

            Assert.That(El3.Occupied, Is.True);
            Assert.That(El3.Rectangle.Position, Is.EqualTo(G.Rectangle.Position));
            Assert.That(El3.Rectangle.Size, Is.EqualTo(new Vector2(5, 3)));

            Assert.That(H.Occupied, Is.False);
            Assert.That(H.Rectangle.Position, Is.EqualTo(G.Rectangle.Position + new Vector2(5, 0)));
            Assert.That(H.Rectangle.Size, Is.EqualTo(new Vector2(2, 3)));

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { D, H, F }),
                        "Free leaves after the third split.");

            // Fourth split
            result = tree.Split(D, new Vector2(4, 4));
            PNode I = D.Left;
            PNode J = D.Right;
            PNode El4 = I.Left;
            PNode K = I.Right;

            Assert.That(result, Is.SameAs(El4), "Fourth split must return the node newly occupied by El4.");

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

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { J, K, H, F }),
                        "Free leaves after the fourth split.");

            // Fifth split
            // perfect match
            result = tree.Split(J, J.Rectangle.Size);

            Assert.That(result, Is.SameAs(J), "A perfectly matching split must return the node itself.");

            Assert.That(J.Occupied, Is.True);
            Assert.That(J.Left, Is.Null);
            Assert.That(J.Right, Is.Null);

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { K, H, F }),
                        "Free leaves after the fifth split.");

            // Sixth split
            // requested rectangle has same width as F
            result = tree.Split(F, new Vector2(F.Rectangle.Size.x, 1));
            PNode Fleft = F.Left;
            PNode Fright = F.Right;

            Assert.That(result, Is.SameAs(Fleft), "Sixth split must return the newly occupied left node of F.");

            Assert.That(Fleft.Occupied, Is.True);
            Assert.That(Fleft.Rectangle.Position, Is.EqualTo(F.Rectangle.Position));
            Assert.That(Fleft.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, 1)));

            Assert.That(Fright.Occupied, Is.False);
            Assert.That(Fright.Rectangle.Position, Is.EqualTo(F.Rectangle.Position + new Vector2(0, Fleft.Rectangle.Size.y)));
            Assert.That(Fright.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, F.Rectangle.Size.y - Fleft.Rectangle.Size.y)));

            Assert.That(tree.FreeLeaves, Is.EquivalentTo(new List<PNode>() { K, H, Fright }),
                        "Free leaves after the sixth split.");
        }

        /// <summary>
        /// Let's us explore performance issues.
        /// </summary>
        [Test]
        public void TestLayout()
        {
            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes();

            RectanglePackingNodeLayout packer = new();

            Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
        }
    }
}

