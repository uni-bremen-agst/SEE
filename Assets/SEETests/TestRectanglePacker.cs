using MoreLinq;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Globalization;
//using ClosedXML.Excel; // NuGet-Paket 'ClosedXML' wird benötigt


using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using UnityEngine;
using static SEE.Layout.RectanglePacking.TestRectanglePacker;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

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

        /*
         Test PTree merge and split operations.
         */

        //*************************************************************************************************************

        [Test]
        public void TestDeleteMergeRemainLeavesAfterGrow()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Left;
            PNode ParentB = A;
            PNode C = A.Right;
            PNode ParentC = A;
            B.Left.Parent = B;
            B.Right.Parent = B;
            PNode El1 = B.Left;
            PNode D = B.Right;

            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            tree.Print();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            tree.GrowLeaf2(El1, new Vector3(1.9f, 1, 1.9f));
            tree.Print();
            //tree.DeleteMergeRemainLeaves2(result);
            tree.Print();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }

        /// <summary>
        /// Runs the example scenario used by Richard Wettel in his dissertation
        /// plus two additions at the end to check situations he did not cover
        /// in this example. See page 36 in "Software Systems as Cities" by
        /// Richard Wettel.
        /// </summary>
        /// 

        //*************************************************************************************************************
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
        }

        //*************************************************************************************************************
        [Test]
        public void TestSplit1()
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

            PNode B = A.Rests[0];
            PNode C = A.Rests[1];
            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];

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
            PNode E = C.Rests[0];
            PNode F = C.Rests[1];
            PNode El2 = E.Rests[0];
            PNode G = E.Rests[1];

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
            PNode El3 = G.Rests[0];
            PNode H = G.Rests[1];

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
            PNode I = D.Rests[0];
            PNode J = D.Rests[1];
            PNode El4 = I.Rests[0];
            PNode K = I.Rests[1];

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
            //Assert.That(J.Rests[0], Is.Null);
            //Assert.That(J.Rests[1], Is.Null);

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, F }), Is.True);

            // Sixth split
            // requested rectangle has same width as F
            result = tree.Split(F, new Vector2(F.Rectangle.Size.x, 1));
            PNode Fleft = F.Rests[0];
            PNode Fright = F.Rests[1];

            Assert.AreSame(result, Fleft);

            Assert.That(Fleft.Occupied, Is.True);
            Assert.That(Fleft.Rectangle.Position, Is.EqualTo(F.Rectangle.Position));
            Assert.That(Fleft.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, 1)));

            Assert.That(Fright.Occupied, Is.False);
            Assert.That(Fright.Rectangle.Position, Is.EqualTo(F.Rectangle.Position + new Vector2(0, Fleft.Rectangle.Size.y)));
            Assert.That(Fright.Rectangle.Size, Is.EqualTo(new Vector2(F.Rectangle.Size.x, F.Rectangle.Size.y - Fleft.Rectangle.Size.y)));

            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { K, H, Fright }), Is.True);

            tree.Print1();
        }

        //*************************************************************************************************************

        [Test]
        public void TestObvious()
        {
            Assert.True(true);
            Vector2 a = new Vector2(5.0f, 5.0f) - new Vector2(10.0f, 10.0f);
            List<string> list1 = new();

            //Debug.Log(a);
            Debug.Log(Mathf.Min(-10f, -5f));
        }

        //*************************************************************************************************************
        [Test]
        public void TestCase1RestPNodeRP2()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);

            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(5, 6);
            Vector2 EL2size = new(5, 6);
            PNode result1 = tree.Split(A, EL1size);


            PNode B = A.Left;
            PNode C = A.Right;
            PNode El1 = B.Left;
            PNode D = B.Right;

            Assert.AreSame(result1, El1);

            PNode result2 = tree.Split(D, EL2size);

            Assert.AreSame(result2, D);

            tree.GrowLeaf2(El1, new Vector3(7.9f, 1, 6.9f));

            tree.Print();

        }
        //************************************************************************************************************
        [Test]
        public void TestCase2RestPNodeRP2()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);

            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(6, 5);
            Vector2 EL2size = new(6, 5);
            PNode result1 = tree.Split(A, EL1size);


            PNode B = A.Left;
            PNode C = A.Right;
            PNode El1 = B.Left;
            PNode D = B.Right;


            Assert.AreSame(result1, El1);

            PNode result2 = tree.Split(C, EL2size);

            PNode E = C.Left;
            PNode F = C.Right;

            Assert.AreSame(result2, E);

            tree.GrowLeaf2(El1, new Vector3(6.9f, 1, 7.9f));

            tree.Print();

        }
        //************************************************************************************************************


        [Test]
        public void TestFreeLeavesAdjust()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Left;
            PNode ParentB = A;
            PNode C = A.Right;
            PNode ParentC = A;
            B.Left.Parent = B;
            B.Right.Parent = B;
            PNode El1 = B.Left;
            PNode D = B.Right;
            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            Vector2 oldRootSize = tree.Root.Rectangle.Size;

            tree.Root.Rectangle.Size = new Vector2(15, 15);
            // Adjust free leaves
            //tree.FreeLeavesAdjust(oldRootSize, A);
            tree.Print();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);
        }

        //************************************************************************************************************
        [Test]
        public void TestFreeLeavesAdjust1()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Rests[0];
            PNode ParentB = A;
            PNode C = A.Rests[1];
            PNode ParentC = A;
            //B.Rests.Add(new());
            //B.Rests.Add(new());
            B.Rests[0].Parent = B;
            B.Rests[1].Parent = B;

            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];
            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            Vector2 oldRootSize = tree.Root.Rectangle.Size;

            tree.Root.Rectangle.Size = new Vector2(15, 15);
            // Adjust free leaves
            //tree.FreeLeavesAdjust1(oldRootSize, A);
            tree.Print1();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);
        }

        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRLNormal()
        {
            /*
            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);
            RectanglePackingNodeLayout2 packer1 = new();
            Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(gameObjects, Vector3.zero, Vector2.one);
            RectanglePackingNodeLayout2 packer2 = new();
            packer2.oldLayout = packer1;
            Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(gameObjects, Vector3.zero, Vector2.one);
             */
            LayoutVertex node1 = new(new Vector3(0.8f, 0.1f, 0.6f), 1);
            LayoutVertex node2 = new(new Vector3(0.7f, 0.1f, 0.3f), 2);
            LayoutVertex node3 = new(new Vector3(0.5f, 0.1f, 0.3f), 3);
            LayoutVertex node4 = new(new Vector3(0.4f, 0.1f, 0.4f), 4);
            ICollection<ILayoutNode> nodes1 = new[] { node1 };
            ICollection<ILayoutNode> nodes2 = new[] { node1, node2 };
            ICollection<ILayoutNode> nodes3 = new[] { node1, node2, node3 };
            ICollection<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };

            IncrementalRectanglePackingLayout packer1 = new();
            IncrementalRectanglePackingLayout packer2 = new();
            IncrementalRectanglePackingLayout packer3 = new();
            IncrementalRectanglePackingLayout packer4 = new();

            Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer2.oldLayout = packer1;

            Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer3.oldLayout = packer2;

            Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes3, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer4.oldLayout = packer3;

            Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);


        }

        //************************************************************************************************************
        [Test]
        public void TestLayoutZSRL1HierachyLayoutVertex()
        {
            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes();

            IncrementalRectanglePackingLayout packer = new();

            Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
        }
        //************************************************************************************************************

        [Test]
        public void TestLayoutZSRL2WithLayoutGraphNode()
        {
            /*
            //RectanglePackingNodeLayout3
            //ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(10, 2);
             */

            Graph graph = new Graph();

            Node node1 = new Node();
            Node node2 = new Node();
            Node node3 = new Node();
            Node node4 = new Node();
            Node node5 = new Node();
            Node node6 = new Node();
            Node node7 = new Node();

            node1.ID = "1";
            node2.ID = "2";
            node3.ID = "3";
            node4.ID = "4";
            node5.ID = "5";
            node6.ID = "6";
            node7.ID = "7";

            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(node3);
            graph.AddNode(node4);
            graph.AddNode(node5);
            graph.AddNode(node6);
            graph.AddNode(node7);

            node1.ItsGraph = graph;
            node2.ItsGraph = graph;
            node3.ItsGraph = graph;
            node4.ItsGraph = graph;
            node5.ItsGraph = graph;
            node6.ItsGraph = graph;
            node7.ItsGraph = graph;


            node1.AddChild(node2);
            node1.AddChild(node3);

            node2.AddChild(node4);
            node2.AddChild(node5);

            node3.AddChild(node6);
            node3.AddChild(node7);

            LayoutGraphNode nodeLayout1 = new LayoutGraphNode(node1);
            LayoutGraphNode nodeLayout2 = new LayoutGraphNode(node2);
            LayoutGraphNode nodeLayout3 = new LayoutGraphNode(node3);
            LayoutGraphNode nodeLayout4 = new LayoutGraphNode(node4);
            LayoutGraphNode nodeLayout5 = new LayoutGraphNode(node5);
            LayoutGraphNode nodeLayout6 = new LayoutGraphNode(node6);
            LayoutGraphNode nodeLayout7 = new LayoutGraphNode(node7);

            nodeLayout1.Parent = null;
            nodeLayout1.AddChild(nodeLayout2);
            nodeLayout1.AddChild(nodeLayout3);

            nodeLayout2.AddChild(nodeLayout4);
            nodeLayout2.AddChild(nodeLayout5);

            nodeLayout3.AddChild(nodeLayout6);
            nodeLayout3.AddChild(nodeLayout7);

            nodeLayout1.AbsoluteScale = new Vector3(0.8f, 0.1f, 0.8f);
            nodeLayout2.AbsoluteScale = new Vector3(0.4f, 0.1f, 0.4f);
            nodeLayout3.AbsoluteScale = new Vector3(0.4f, 0.1f, 0.4f);
            nodeLayout4.AbsoluteScale = new Vector3(0.2f, 0.1f, 0.2f);
            nodeLayout5.AbsoluteScale = new Vector3(0.2f, 0.1f, 0.2f);
            nodeLayout6.AbsoluteScale = new Vector3(0.2f, 0.1f, 0.2f);
            nodeLayout7.AbsoluteScale = new Vector3(0.2f, 0.1f, 0.2f);

            /*
            NodeTransform nt1 = new NodeTransform(Vector3.zero, nodeLayout1.AbsoluteScale);
            NodeTransform nt2 = new NodeTransform(Vector3.zero, nodeLayout2.AbsoluteScale);
            NodeTransform nt3 = new NodeTransform(Vector3.zero, nodeLayout3.AbsoluteScale);
            NodeTransform nt4 = new NodeTransform(Vector3.zero, nodeLayout4.AbsoluteScale);
            NodeTransform nt5 = new NodeTransform(Vector3.zero, nodeLayout5.AbsoluteScale);
            NodeTransform nt6 = new NodeTransform(Vector3.zero, nodeLayout6.AbsoluteScale);
            NodeTransform nt7 = new NodeTransform(Vector3.zero, nodeLayout7.AbsoluteScale);

            Dictionary<ILayoutNode, NodeTransform> layout = new Dictionary<ILayoutNode, NodeTransform>()
            {
              { nodeLayout1, nt1 },
              { nodeLayout2, nt2 },
              { nodeLayout3, nt3 },
              { nodeLayout4, nt4 },
              { nodeLayout5, nt5 },
              { nodeLayout6, nt6 },
              { nodeLayout7, nt7 }
            };
             */


            IncrementalRectanglePackingLayout packer = new();


            List<ILayoutNode> gameObjects = new List<ILayoutNode>() { nodeLayout1, nodeLayout2, nodeLayout3, nodeLayout4, nodeLayout5, nodeLayout6, nodeLayout7 };


            Dictionary<ILayoutNode, NodeTransform> layout1 = packer.Create(gameObjects, Vector3.zero, Vector2.one);


        }

        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRL3GrowLeafWithPacker()
        {
            /*
            //Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
            Dictionary<ILayoutNode, NodeTransform> secondLayout = new Dictionary<ILayoutNode, NodeTransform>();
             */
            LayoutVertex node1 = new(new Vector3(8.0f, 1, 6.0f), 1);
            LayoutVertex node2 = new(new Vector3(7.0f, 1, 3.0f), 2);
            LayoutVertex node3 = new(new Vector3(5.0f, 1, 3.0f), 3);
            LayoutVertex node4 = new(new Vector3(4.0f, 1, 4.0f), 4);
            IEnumerable<ILayoutNode> nodes1 = new[] { node1 };
            IEnumerable<ILayoutNode> nodes2 = new[] { node1, node2 };
            IEnumerable<ILayoutNode> nodes3 = new[] { node1, node2, node3 };
            IEnumerable<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };

            IncrementalRectanglePackingLayout packer1 = new();
            IncrementalRectanglePackingLayout packer2 = new();
            IncrementalRectanglePackingLayout packer3 = new();
            IncrementalRectanglePackingLayout packer4 = new();

            Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

            packer2.oldLayout = packer1;

            Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

            packer3.oldLayout = packer2;

            Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes4, Vector3.zero, Vector2.one);

            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in packer3.layoutResult.ToList())
            {
                if (entry.Key.ID == "1")
                {
                    Debug.Log("here asdfasdf");
                    //ILayoutNode vertex = new LayoutVertex(new Vector3(3.0f, 1, 6.0f), 1);
                    // Remove the old key and add the new key-value pair
                    //packer3.layoutResult.Remove(entry.Key);
                    //packer3.layoutResult[vertex] = entry.Value;
                    entry.Key.AbsoluteScale = new Vector3(3.0f, 1, 3.0f);
                    Debug.LogFormat("Updated layout for node ID {0}: Size: {1}\n",
                        entry.Key.ID,
                        entry.Key.AbsoluteScale);
                }
                if (entry.Key.ID == "4")
                {
                    Debug.Log("here asdfasdf");
                    //ILayoutNode vertex = new LayoutVertex(new Vector3(3.0f, 1, 6.0f), 1);
                    // Remove the old key and add the new key-value pair
                    //packer3.layoutResult.Remove(entry.Key);
                    //packer3.layoutResult[vertex] = entry.Value;
                    entry.Key.AbsoluteScale = new Vector3(3.0f, 1, 3.0f);
                    Debug.LogFormat("Updated layout for node ID {0}: Size: {1}\n",
                        entry.Key.ID,
                        entry.Key.AbsoluteScale);
                }
            }
            /*
             */
            packer4.oldLayout = packer3;

            Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);


        }
        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRL3GrowLeafWithPacker1()
        {
            /*
            //Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
            Dictionary<ILayoutNode, NodeTransform> secondLayout = new Dictionary<ILayoutNode, NodeTransform>();
             */
            LayoutVertex node1 = new(new Vector3(0.8f, 1, 0.6f), 1);
            LayoutVertex node2 = new(new Vector3(0.7f, 1, 0.3f), 2);
            LayoutVertex node3 = new(new Vector3(0.5f, 1, 0.3f), 3);
            LayoutVertex node4 = new(new Vector3(0.4f, 1, 0.4f), 4);
            LayoutVertex node5 = new(new Vector3(0.1f, 1, 0.3f), 5);

            //IEnumerable<ILayoutNode> nodes1 = new[] { node1, node2, node3, node4 };
            //IEnumerable<ILayoutNode> nodes2 = new[] { node1, node2 };
            IEnumerable<ILayoutNode> nodes3 = new[] { node1, node2, node3, node4 };
            IEnumerable<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };
            IEnumerable<ILayoutNode> nodes5 = new[] { node1, node2, node3, node4, node5 };


            //IncrementalRectanglePackingLayout packer1 = new();
            //IncrementalRectanglePackingLayout packer2 = new();
            IncrementalRectanglePackingLayout packer3 = new();
            IncrementalRectanglePackingLayout packer4 = new();
            IncrementalRectanglePackingLayout packer5 = new();


            //Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

            //packer2.oldLayout = packer1;

            //Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

            //packer3.oldLayout = packer2;

            Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes4, Vector3.zero, Vector2.one);

            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in packer3.layoutResult.ToList())
            {
                if (entry.Key.ID == "1")
                {
                    Debug.Log("here asdfasdf");
                    //ILayoutNode vertex = new LayoutVertex(new Vector3(3.0f, 1, 6.0f), 1);
                    // Remove the old key and add the new key-value pair
                    //packer3.layoutResult.Remove(entry.Key);
                    //packer3.layoutResult[vertex] = entry.Value;
                    entry.Key.AbsoluteScale = new Vector3(0.9f, 1, 0.7f);
                    Debug.LogFormat("Updated layout for node ID {0}: Size: {1}\n",
                        entry.Key.ID,
                        entry.Key.AbsoluteScale);
                }

            }
            /*
             */
            packer4.oldLayout = packer3;

            Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);

            packer5.oldLayout = packer4;

            Dictionary<ILayoutNode, NodeTransform> fifthLayout = packer5.Create(nodes5, Vector3.zero, Vector2.one);


        }

        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL4DeleteMergeRemainLeaves()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Rests[0];
            PNode ParentB = A;
            PNode C = A.Rests[1];
            PNode ParentC = A;

            //B.Rests[0].Parent = B;
            //B.Rests[1].Parent = B;
            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];

            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            //tree.Print1();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            //tree.GrowLeaf(El1, new Vector3(1.9f, 1, 1.9f));
            //tree.Print();
            //tree.DeleteMergeRemainLeaves1(result);
            Debug.Log("---------------------------------------------------------------------------------------------------------------");
            tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }

        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL5GrowLeaf00()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            //PNode B = A.Rests[0];
            //PNode ParentB = A;
            //PNode C = A.Rests[1];
            //PNode D = A.Rests[2];

            //PNode ParentC = A;

            //B.Rests[0].Parent = B;
            //B.Rests[1].Parent = B;
            //PNode El1 = B.Rests[0];
            //PNode D = B.Rests[1];

            //Assert.AreSame(result, B);
            Debug.Log(tree.FreeLeaves.Count);
            //tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            //tree.GrowLeaf(A.Rests[0], new Vector3(2.0f, 1, 2.0f));
            //tree.Print();
            //tree.DeleteMergeRemainLeaves1(result);
            Debug.Log("---------------------------------------------------------------------------------------------------------------");
            tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }

        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL5GrowLeaf01()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Rests[0];
            PNode ParentB = A;
            PNode C = A.Rests[1];
            PNode ParentC = A;

            //B.Rests[0].Parent = B;
            //B.Rests[1].Parent = B;
            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];

            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            //tree.Print1();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            tree.GrowLeaf2(El1, new Vector3(2.0f, 1, 6.0f));
            //tree.Print();
            //tree.DeleteMergeRemainLeaves1(result);
            Debug.Log("---------------------------------------------------------------------------------------------------------------");
            tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }

        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL5GrowLeaf10()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Rests[0];
            PNode ParentB = A;
            PNode C = A.Rests[1];
            PNode ParentC = A;

            //B.Rests[0].Parent = B;
            //B.Rests[1].Parent = B;
            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];

            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            //tree.Print1();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            tree.GrowLeaf2(El1, new Vector3(6.0f, 1, 2.0f));
            //tree.Print();
            //tree.DeleteMergeRemainLeaves1(result);
            Debug.Log("---------------------------------------------------------------------------------------------------------------");
            tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }

        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL5GrowLeaf11()
        {
            Vector2 totalSize = new(10, 10);
            PTree tree = new(Vector2.zero, totalSize);
            PNode A = tree.Root;
            PNode ParentA = null;
            Assert.That(A.Occupied, Is.False);
            Assert.That(A.Rectangle.Position, Is.EqualTo(Vector2.zero));
            Assert.That(A.Rectangle.Size, Is.EqualTo(totalSize));

            // First split
            Vector2 EL1size = new(4, 4);
            PNode result = tree.Split(A, EL1size);
            tree.FreeLeaves.Remove(A);
            PNode B = A.Rests[0];
            PNode ParentB = A;
            PNode C = A.Rests[1];
            PNode ParentC = A;

            //B.Rests[0].Parent = B;
            //B.Rests[1].Parent = B;
            PNode El1 = B.Rests[0];
            PNode D = B.Rests[1];

            Assert.AreSame(result, El1);
            Debug.Log(tree.FreeLeaves.Count);
            //tree.Print1();
            Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

            // Merge El1
            // test with rest

            //Vector2 restSize = new(2,2);
            tree.GrowLeaf2(El1, new Vector3(6.0f, 1, 6.0f));
            //tree.Print();
            //tree.DeleteMergeRemainLeaves1(result);
            Debug.Log("---------------------------------------------------------------------------------------------------------------");
            tree.Print1();
            //Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

        }
        //*************************************************************************************************************

        [Test]
        public void TestLayoutZSRL6NewPackingAlgorithm()
        {
            /*
            ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);
            RectanglePackingNodeLayout2 packer1 = new();
            Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(gameObjects, Vector3.zero, Vector2.one);
            RectanglePackingNodeLayout2 packer2 = new();
            packer2.oldLayout = packer1;
            Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(gameObjects, Vector3.zero, Vector2.one);
             */
            LayoutVertex node1 = new(new Vector3(0.8f, 0.1f, 0.6f), 1);
            LayoutVertex node2 = new(new Vector3(0.7f, 0.1f, 0.3f), 2);
            LayoutVertex node3 = new(new Vector3(0.5f, 0.1f, 0.3f), 3);
            LayoutVertex node4 = new(new Vector3(0.4f, 0.1f, 0.4f), 4);
            LayoutVertex node5 = new(new Vector3(0.6f, 0.1f, 0.3f), 5);

            ICollection<ILayoutNode> nodes1 = new[] { node1 };
            ICollection<ILayoutNode> nodes2 = new[] { node1, node2 };
            ICollection<ILayoutNode> nodes3 = new[] { node1, node2, node3 };
            ICollection<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };
            ICollection<ILayoutNode> nodes5 = new[] { node1, node2, node3, node4, node5 };

            IncrementalRectanglePackingLayout packer1 = new();
            IncrementalRectanglePackingLayout packer2 = new();
            IncrementalRectanglePackingLayout packer3 = new();
            IncrementalRectanglePackingLayout packer4 = new();
            IncrementalRectanglePackingLayout packer5 = new();

            Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer2.oldLayout = packer1;

            Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer3.oldLayout = packer2;

            Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes3, Vector3.zero, Vector2.one);

            //RectanglePackingNodeLayout2.tree.Print();

            packer4.oldLayout = packer3;

            Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);

            packer5.oldLayout = packer4;

            Dictionary<ILayoutNode, NodeTransform> fifthLayout = packer5.Create(nodes5, Vector3.zero, Vector2.one);

        }

        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRL7GrowLeafWithPackerTighten()
        {
            /*
            //Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
            Dictionary<ILayoutNode, NodeTransform> secondLayout = new Dictionary<ILayoutNode, NodeTransform>();
             */
            LayoutVertex node1 = new(new Vector3(0.8f, 1, 0.6f), 1);
            LayoutVertex node2 = new(new Vector3(0.7f, 1, 0.3f), 2);
            LayoutVertex node3 = new(new Vector3(0.5f, 1, 0.3f), 3);
            LayoutVertex node4 = new(new Vector3(0.4f, 1, 0.4f), 4);
            LayoutVertex node5 = new(new Vector3(0.1f, 1, 0.3f), 5);

            //IEnumerable<ILayoutNode> nodes1 = new[] { node1, node2, node3, node4 };
            //IEnumerable<ILayoutNode> nodes2 = new[] { node1, node2 };
            IEnumerable<ILayoutNode> nodes3 = new[] { node1, node2, node3, node4 };
            IEnumerable<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };
            //IEnumerable<ILayoutNode> nodes5 = new[] { node1, node2, node3, node4, node5 };


            //IncrementalRectanglePackingLayout packer1 = new();
            //IncrementalRectanglePackingLayout packer2 = new();
            IncrementalRectanglePackingLayout packer3 = new();
            IncrementalRectanglePackingLayout packer4 = new();
            //IncrementalRectanglePackingLayout packer5 = new();


            //Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

            //packer2.oldLayout = packer1;

            //Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

            //packer3.oldLayout = packer2;

            Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes3, Vector3.zero, Vector2.one);

            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in packer3.layoutResult.ToList())
            {
                if (entry.Key.ID == "1")
                {
                    Debug.Log("here asdfasdf");
                    //ILayoutNode vertex = new LayoutVertex(new Vector3(3.0f, 1, 6.0f), 1);
                    // Remove the old key and add the new key-value pair
                    //packer3.layoutResult.Remove(entry.Key);
                    //packer3.layoutResult[vertex] = entry.Value;
                    entry.Key.AbsoluteScale = new Vector3(0.9f, 1, 0.7f);
                    Debug.LogFormat("Updated layout for node ID {0}: Size: {1}\n",
                        entry.Key.ID,
                        entry.Key.AbsoluteScale);
                }

            }
            /*
             */
            packer4.oldLayout = packer3;

            Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);

            //packer5.oldLayout = packer4;

            //Dictionary<ILayoutNode, NodeTransform> fifthLayout = packer5.Create(nodes5, Vector3.zero, Vector2.one);


        }

        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRL7GrowLeafWithPackerTightenTestOverLap()
        {
            PNode root = new PNode(Vector2.zero, new Vector2(10, 10));
            PNode leaf1 = new PNode(new Vector2(5, 5), new Vector2(2, 2));
            PNode leaf2 = new PNode(new Vector2(2, 6), new Vector2(2, 2));
            PNode leaf3 = new PNode(new Vector2(7, 2), new Vector2(2, 2));

            PTree tree = new(Vector2.zero, new Vector2(10, 10));
            tree.Root = root;
            tree.Root.Rests.Add(leaf1);
            tree.Root.Rests.Add(leaf2);
            tree.Root.Rests.Add(leaf3);

            tree.Tighten(tree.Root);

            tree.Print1();


        }

        //*************************************************************************************************************
        [Test]
        public void TestLayoutZSRL8TestPlacesNodesWithLayoutVertex()
        {
            Dictionary<ILayoutNode, NodeTransform> layout = new Dictionary<ILayoutNode, NodeTransform>();
            LayoutVertex root = new("root");

            LayoutVertex root1 = new("root1");

            LayoutVertex root2 = new("root2");

            LayoutVertex root3 = new("root3");

            LayoutVertex root4 = new("root4");


            LayoutVertex leaf1 = new(new Vector3(0.2f, 0.1f, 0.2f), 4);
            LayoutVertex leaf2 = new(new Vector3(0.2f, 0.1f, 0.2f), 5);
            LayoutVertex leaf3 = new(new Vector3(0.2f, 0.1f, 0.2f), 6);
            LayoutVertex leaf4 = new(new Vector3(0.2f, 0.1f, 0.2f), 7);

            LayoutVertex leaf5 = new(new Vector3(0.2f, 0.1f, 0.2f), 4);
            LayoutVertex leaf6 = new(new Vector3(0.2f, 0.1f, 0.2f), 5);
            LayoutVertex leaf7 = new(new Vector3(0.2f, 0.1f, 0.2f), 6);
            LayoutVertex leaf8 = new(new Vector3(0.2f, 0.1f, 0.2f), 7);

            root.AddChild(root1);
            root.AddChild(root2);

            root1.AddChild(leaf1);
            root1.AddChild(leaf2);

            root2.AddChild(leaf3);
            root2.AddChild(leaf4);


            IList<ILayoutNode> layoutNodeList = new List<ILayoutNode> { root, root1, root2, leaf1, leaf2, leaf3, leaf4 };

            IncrementalRectanglePackingLayout packer = new();
            IncrementalRectanglePackingLayout packer2 = new();




            //layout[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
            packer.Create(layoutNodeList, Vector3.zero, Vector2.one);

            //Debug.LogFormat("Placed root at position {0}{1} with size {2}\n", layout[root].X, layout[root].Z, layout[root].Scale);

            //////////////////////////////////////////////////////////////////////////////////////////////
            Debug.Log("//////////////////////////////////////////////////////////");
            layoutNodeList = new List<ILayoutNode> { root, root1, root2, root3, root4, leaf1, leaf2, leaf3, leaf4, leaf5, leaf6, leaf7, leaf8 };

            packer2.oldLayout = packer;

            root.AddChild(root3);
            root.AddChild(root4);

            root3.AddChild(leaf5);
            root3.AddChild(leaf6);

            root4.AddChild(leaf7);
            root4.AddChild(leaf8);


            packer2.Create(layoutNodeList, Vector3.zero, Vector2.one);






        }

        //*************************************************************************************************************

        #region Evaluation Tests
        [Test]
        public void JustTest()
        {
            List<List<LayoutVertex>> layoutVertexGroups = new List<List<LayoutVertex>>();
            List<LayoutVertex> layoutVertices = new List<LayoutVertex>();


            for (int j = 0; j < 100; j++)
            {
                layoutVertices = new List<LayoutVertex>();
                for (int i = 0; i < Random.Range(1, 101); i++)
                {
                    layoutVertices.Add(new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), i));
                }
                layoutVertexGroups.Add(layoutVertices);
            }

            List<IncrementalRectanglePackingLayout> packers = new List<IncrementalRectanglePackingLayout>();
            for (int i = 0; i < 100; i++)
            {
                packers.Add(new IncrementalRectanglePackingLayout());
                if (i > 0)
                {
                    packers[i].oldLayout = packers[i - 1];
                }
            }

            for (int i = 0; i < 100; i++)
            {
                packers[i].Create(layoutVertexGroups[i], Vector3.zero, Vector2.one);
            }


        }

        //////////////////////////////////////////////////////////////////////////////

        #region bullshitcalculations
        public float CalculateEuclideanMentalDistance(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            float totalDistance = 0f;

            // Schritt 1: Erstelle ein temporäres Dictionary für Layout 2, 
            // um Knoten blitzschnell anhand ihrer String-ID (O(1)) zu finden.
            Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
            foreach (var kvp in layout2)
            {
                layout2ById[kvp.Key.ID] = kvp.Value;
            }

            // Schritt 2: Iteriere über alle Knoten der ersten Revision
            foreach (var kvp in layout1)
            {
                string nodeId = kvp.Key.ID;
                NodeTransform transform1 = kvp.Value;

                // Schritt 3: Prüfe auf die Schnittmenge (V1 \cap V2)
                // Gibt es diesen Knoten auch in der zweiten Revision?
                if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
                {
                    // Extrahiere die Positionen (Vector3 oder Vector2, je nach Implementierung)
                    // Hier nehme ich an, NodeTransform hat ein Feld 'position'
                    Vector2 pos1 = new Vector2(transform1.CenterPosition.x, transform1.CenterPosition.z);
                    Vector2 pos2 = new Vector2(transform2.CenterPosition.x, transform2.CenterPosition.z);

                    // Schritt 4: Berechne die euklidische Distanz
                    float distance = Vector2.Distance(pos1, pos2);
                    // Schritt 5: Addiere zur Gesamtsumme
                    totalDistance += distance;
                }

                if (totalDistance == 0)
                {
                    //Debug.Log(nodeId);
                }
            }

            return totalDistance;
        }

        public float CalculateADN(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            float totalDistance = 0f;
            int intersectionCount = 0; // Zählt die Bestandsknoten

            Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
            foreach (var kvp in layout2)
            {
                layout2ById[kvp.Key.ID] = kvp.Value;
            }

            foreach (var kvp in layout1)
            {
                string nodeId = kvp.Key.ID;
                if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
                {
                    Vector3 pos1 = kvp.Value.CenterPosition;
                    Vector3 pos2 = transform2.CenterPosition;

                    Vector2 pos1_2D = new Vector2(pos1.x, pos1.z);
                    Vector2 pos2_2D = new Vector2(pos2.x, pos2.z);

                    totalDistance += Vector2.Distance(pos1_2D, pos2_2D);
                    intersectionCount++; // Knoten ist in beiden Layouts
                }
            }

            // Division durch Null abfangen (falls es keine gemeinsamen Knoten gibt)
            if (intersectionCount == 0) return 0f;

            // ADN = Gesamtdistanz geteilt durch die Anzahl der Bestandsknoten
            return totalDistance / intersectionCount;
        }

        public float CalculateAverageRelativeDistance(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            // Schritt 1: Wir sammeln alle Bestandsknoten in synchronen Listen,
            // um den O(n^2) Vergleich extrem schnell via Index machen zu können.
            List<NodeTransform> commonNodes1 = new List<NodeTransform>();
            List<NodeTransform> commonNodes2 = new List<NodeTransform>();

            Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
            foreach (var kvp in layout2)
            {
                layout2ById[kvp.Key.ID] = kvp.Value;
            }

            foreach (var kvp in layout1)
            {
                string nodeId = kvp.Key.ID;
                if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
                {
                    commonNodes1.Add(kvp.Value); // Zustand in Revision 1
                    commonNodes2.Add(transform2); // Zustand in Revision 2
                }
            }

            int n = commonNodes1.Count;

            // Wenn es weniger als 2 gemeinsame Knoten gibt, 
            // kann keine relative Distanz zueinander gemessen werden.
            if (n <= 1) return 0f;

            float totalRelativeDistance = 0f;

            // Schritt 2: Doppelte Iteration über alle Knotenpaare (v, w)
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // Ein Knoten kann nicht mit sich selbst verglichen werden
                    if (i == j) continue;

                    // Distanz ZWISCHEN Knoten i und j in der ALTEN Revision
                    Vector2 pos1_i = new Vector2(commonNodes1[i].CenterPosition.x, commonNodes1[i].CenterPosition.z);
                    Vector2 pos1_j = new Vector2(commonNodes1[j].CenterPosition.x, commonNodes1[j].CenterPosition.z);
                    float dist1 = Vector2.Distance(pos1_i, pos1_j);

                    // Distanz ZWISCHEN denselben Knoten in der NEUEN Revision
                    Vector2 pos2_i = new Vector2(commonNodes2[i].CenterPosition.x, commonNodes2[i].CenterPosition.z);
                    Vector2 pos2_j = new Vector2(commonNodes2[j].CenterPosition.x, commonNodes2[j].CenterPosition.z);
                    float dist2 = Vector2.Distance(pos2_i, pos2_j);

                    // Addiere den Betrag der Veränderung
                    totalRelativeDistance += Mathf.Abs(dist1 - dist2);
                }
            }

            // Schritt 3: Normalisierung durch (n^2 - n)
            // Hinweis: Bei floats aufpassen, dass n*n nicht den int-Bereich sprengt, 
            // daher sicherheitshalber nach float casten.
            float normalizationFactor = ((float)n * (float)n) - (float)n;

            return totalRelativeDistance / normalizationFactor;
        }

        /// <summary>
        /// Berechnet die "Layout Distance Change".
        /// Bezieht neben der Position auch die Änderungen von Breite und Höhe (Scale) ein.
        /// </summary>
        public float CalculateLayoutDistanceChange(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            float totalDistance = 0f;
            int intersectionCount = 0;

            Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
            foreach (var kvp in layout2)
            {
                layout2ById[kvp.Key.ID] = kvp.Value;
            }

            foreach (var kvp in layout1)
            {
                string nodeId = kvp.Key.ID;
                if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
                {
                    NodeTransform transform1 = kvp.Value;

                    // Delta Position (dx, dy)
                    // Falls du in Unity die X/Z-Achse nutzt, ändere .y hier zu .z
                    float dx = transform1.CenterPosition.x - transform2.CenterPosition.x;
                    float dy = transform1.CenterPosition.z - transform2.CenterPosition.z;

                    // Delta Dimensionen (dw, dh)
                    // Angenommen, 'scale' ist dein Vector3 für die Größe
                    float dw = transform1.Scale.x - transform2.Scale.x;
                    float dh = transform1.Scale.z - transform2.Scale.z;

                    // Formel: Wurzel aus (dx^2 + dy^2 + dw^2 + dh^2)
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy) + (dw * dw) + (dh * dh));

                    totalDistance += distance;
                    intersectionCount++;
                }
            }

            if (intersectionCount == 0) return 0f;

            // Rückgabe als Durchschnitt (normalisiert durch Knotenanzahl)
            return totalDistance / intersectionCount;
        }


        //*************************************************************************************************************
        /// <summary>
        /// Berechnet die "Nearest Neighbor Within" (NNW) Metrik.
        /// Zählt, bei wie vielen Knoten sich der direkteste Nachbar verändert hat.
        /// Gibt einen Wert zwischen 0 (perfekter Erhalt) und 1 (komplette Zerstörung) zurück.
        /// </summary>
        public float CalculateNearestNeighborWithin(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            // Schritt 1: Dictionaries für extrem schnellen O(1) ID-Zugriff aufbauen
            Dictionary<string, NodeTransform> l1ById = new Dictionary<string, NodeTransform>();
            Dictionary<string, NodeTransform> l2ById = new Dictionary<string, NodeTransform>();

            foreach (var kvp in layout1) l1ById[kvp.Key.ID] = kvp.Value;
            foreach (var kvp in layout2) l2ById[kvp.Key.ID] = kvp.Value;

            // Schritt 2: Die Schnittmenge (Bestandsknoten) ermitteln
            List<string> commonNodeIds = new List<string>();
            foreach (string id in l1ById.Keys)
            {
                if (l2ById.ContainsKey(id))
                {
                    commonNodeIds.Add(id);
                }
            }

            int n = commonNodeIds.Count;

            // Wenn es weniger als 2 Knoten gibt, gibt es keine Nachbarn
            if (n <= 1) return 0f;

            int brokenNeighborhoods = 0;

            // Schritt 3: Für jeden Bestandsknoten den nächsten Nachbarn in BEIDEN Layouts finden
            foreach (string currentId in commonNodeIds)
            {
                string nearestInL1 = GetNearestNeighborId(currentId, commonNodeIds, l1ById);
                string nearestInL2 = GetNearestNeighborId(currentId, commonNodeIds, l2ById);

                // Wenn sich die ID des nächsten Nachbarn geändert hat -> Bruch der Nachbarschaft!
                if (nearestInL1 != nearestInL2)
                {
                    brokenNeighborhoods++;
                }
            }

            // Schritt 4: Normalisierung
            // Teilt die Anzahl der Brüche durch die Gesamtanzahl der Bestandsknoten
            return (float)brokenNeighborhoods / n;
        }

        /// <summary>
        /// Hilfsfunktion: Findet die ID des nächsten Nachbarn für einen bestimmten Knoten.
        /// Durchsucht dabei AUSSCHLIESSLICH die übergebene Liste der Bestandsknoten.
        /// </summary>
        private string GetNearestNeighborId(string targetId, List<string> validIds, Dictionary<string, NodeTransform> layout)
        {
            float minDistance = float.MaxValue;
            string nearestId = null;

            Vector2 targetPos = new Vector2(layout[targetId].CenterPosition.x, layout[targetId].CenterPosition.z);

            foreach (string otherId in validIds)
            {
                // Einen Knoten nicht mit sich selbst vergleichen
                if (otherId == targetId) continue;

                Vector2 otherPos = new Vector2(layout[otherId].CenterPosition.x, layout[otherId].CenterPosition.z);
                float dist = Vector2.Distance(targetPos, otherPos);

                // Neuer nächster Nachbar gefunden?
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestId = otherId;
                }
            }

            return nearestId;
        }


        //////////////////////////////////////////////////////////

        /// <summary>
        /// Berechnet die "Ranking" (rnk) Metrik.
        /// Misst den Erhalt der orthogonalen Struktur (Rechts/Links, Oben/Unten).
        /// </summary>
        public float CalculateRanking(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            // Schritt 1: Schnittmenge (Bestandsknoten) herausfiltern für synchronen Index-Zugriff
            List<NodeTransform> commonNodes1 = new List<NodeTransform>();
            List<NodeTransform> commonNodes2 = new List<NodeTransform>();

            Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
            foreach (var kvp in layout2) layout2ById[kvp.Key.ID] = kvp.Value;

            foreach (var kvp in layout1)
            {
                if (layout2ById.TryGetValue(kvp.Key.ID, out NodeTransform transform2))
                {
                    commonNodes1.Add(kvp.Value);
                    commonNodes2.Add(transform2);
                }
            }

            int vCount = commonNodes1.Count;

            // Wenn weniger als 2 Knoten existieren, gibt es keine zueinander in Relation stehenden Knoten
            if (vCount <= 1) return 0f;

            // Schritt 2: Upper Bound (UB) berechnen nach der Formel: 1.5 * (|V| - 1)
            float UB = 1.5f * (vCount - 1);

            float totalRankingSum = 0f;

            // Schritt 3: Für jeden Knoten v seinen orthogonalen Rang berechnen
            for (int i = 0; i < vCount; i++)
            {
                Vector2 pos1_i = new Vector2(commonNodes1[i].CenterPosition.x, commonNodes1[i].CenterPosition.z);
                Vector2 pos2_i = new Vector2(commonNodes2[i].CenterPosition.x, commonNodes2[i].CenterPosition.z);

                int rg1 = 0;  // Anzahl der Knoten RECHTS von v in Layout 1
                int abv1 = 0; // Anzahl der Knoten OBERHALB von v in Layout 1

                int rg2 = 0;  // Anzahl der Knoten RECHTS von v in Layout 2
                int abv2 = 0; // Anzahl der Knoten OBERHALB von v in Layout 2

                // Iteriere über alle ANDEREN Knoten, um den Rang zu bestimmen
                for (int j = 0; j < vCount; j++)
                {
                    if (i == j) continue; // v nicht mit sich selbst vergleichen

                    Vector2 pos1_j = new Vector2(commonNodes1[j].CenterPosition.x, commonNodes1[j].CenterPosition.z);
                    Vector2 pos2_j = new Vector2(commonNodes2[j].CenterPosition.x, commonNodes2[j].CenterPosition.z);

                    // --- Auswertung Layout 1 ---
                    if (pos1_j.x > pos1_i.x) rg1++;

                    // Hinweis: Falls dein 2D-Layout in Unity auf dem "Boden" liegt (X/Z-Achse), 
                    // musst du hier .y in .z ändern!
                    if (pos1_j.y > pos1_i.y) abv1++;

                    // --- Auswertung Layout 2 ---
                    if (pos2_j.x > pos2_i.x) rg2++;
                    if (pos2_j.y > pos2_i.y) abv2++;
                }

                // Schritt 4: Die absolute Differenz der Ränge berechnen
                float diffRg = Mathf.Abs(rg1 - rg2);
                float diffAbv = Mathf.Abs(abv1 - abv2);
                float currentDiff = diffRg + diffAbv;

                // Laut Formel wird die Differenz durch die Upper Bound (UB) gecappt (min-Funktion)
                float cappedDiff = Mathf.Min(currentDiff, UB);

                // Zur Gesamtsumme addieren
                totalRankingSum += cappedDiff;
            }

            // Schritt 5: Gemäß Steinbrückner wird die Summe durch UB normalisiert
            // (bzw. durch UB * vCount, um einen Wert für den "durchschnittlichen" Zerstörungsgrad zu erhalten)
            return totalRankingSum / UB;
        }

        //*************************************************************************************************************

        /// <summary>
        /// Berechnet die Smallest Enclosing Rectangle Compactness (SERC).
        /// Gibt den prozentualen Anteil (0-100) der genutzten Fläche im Hüllrechteck zurück.
        /// </summary>
        public float CalculateSERC(Dictionary<ILayoutNode, NodeTransform> layout)
        {
            if (layout.Count == 0) return 0f;

            float totalNodeArea = 0f;

            // Variablen für die Extrempunkte des globalen Hüllrechtecks (Bounding Box)
            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;

            foreach (var kvp in layout)
            {
                NodeTransform t = kvp.Value;

                // Ausdehnung des aktuellen Rechtecks
                float width = t.Scale.x;
                float height = t.Scale.z;

                // 1. Fläche des Rechtecks zur Gesamtsumme addieren
                totalNodeArea += (width * height);

                // 2. Die vier Außenkanten dieses Rechtecks berechnen
                // (Annahme: t.CenterPosition ist der Mittelpunkt des Rechtecks)
                float leftEdge = t.CenterPosition.x - (width / 2f);
                float rightEdge = t.CenterPosition.x + (width / 2f);
                float bottomEdge = t.CenterPosition.z - (height / 2f);
                float topEdge = t.CenterPosition.z + (height / 2f);

                // 3. Globale Bounding Box bei Bedarf erweitern
                if (leftEdge < minX) minX = leftEdge;
                if (rightEdge > maxX) maxX = rightEdge;
                if (bottomEdge < minZ) minZ = bottomEdge;
                if (topEdge > maxZ) maxZ = topEdge;
            }

            // Fläche des ermittelten Hüllrechtecks berechnen
            float boundingBoxWidth = maxX - minX;
            float boundingBoxHeight = maxZ - minZ;
            float boundingBoxArea = boundingBoxWidth * boundingBoxHeight;

            // Division durch Null abfangen
            if (boundingBoxArea <= 0f) return 0f;

            // SERC-Formel: 100 * (A_N / A_R)
            return 100f * (totalNodeArea / boundingBoxArea);
        }

        /// <summary>
        /// Berechnet die Smallest Enclosing Circle Compactness (SECC).
        /// Gibt den prozentualen Anteil (0-100) der genutzten Fläche im Hüllkreis zurück.
        /// </summary>
        /// <param name="layout">Das berechnete Kreis-Layout</param>
        /// <param name="layoutCenter">Das Zentrum des Layouts (meist Vector3.zero)</param>
        public float CalculateSECC(Dictionary<ILayoutNode, NodeTransform> layout, Vector3 layoutCenter)
        {
            if (layout.Count == 0) return 0f;

            float totalNodeArea = 0f;
            float maxRadiusFromCenter = 0f;

            foreach (var kvp in layout)
            {
                NodeTransform t = kvp.Value;

                // Der Radius des aktuellen Knotens (Annahme: Scale.x ist der Durchmesser)
                float nodeRadius = t.Scale.x / 2f;

                // 1. Fläche dieses Kreises zur Gesamtsumme addieren (Pi * r^2)
                totalNodeArea += Mathf.PI * nodeRadius * nodeRadius;

                // 2. Distanz des Kreismittelpunkts zum globalen Layout-Zentrum
                // Wir ignorieren die Y-Achse (Höhe), da wir die Grundfläche evaluieren
                Vector3 pos2D = new Vector3(t.CenterPosition.x, 0, t.CenterPosition.z);
                Vector3 center2D = new Vector3(layoutCenter.x, 0, layoutCenter.z);

                float distanceToCenter = Vector3.Distance(pos2D, center2D);

                // 3. Die äußerste Kante dieses Kreises vom Zentrum aus gesehen
                float outerEdgeDistance = distanceToCenter + nodeRadius;

                // Wenn diese Kante weiter außen liegt als alle bisherigen, 
                // haben wir einen neuen maximalen Hüllkreis-Radius gefunden
                if (outerEdgeDistance > maxRadiusFromCenter)
                {
                    maxRadiusFromCenter = outerEdgeDistance;
                }
            }

            // Fläche des minimal umschließenden Hüllkreises berechnen (Pi * R^2)
            float enclosingCircleArea = Mathf.PI * maxRadiusFromCenter * maxRadiusFromCenter;

            // Division durch Null abfangen
            if (enclosingCircleArea <= 0f) return 0f;

            // SECC-Formel: 100 * (A_N / A_C)
            return 100f * (totalNodeArea / enclosingCircleArea);
        }

        //*************************************************************************************************************
        /// <summary>
        /// Berechnet die "Relative Weight Change" (RWC).
        /// Misst das Ausmaß der strukturellen Größenänderung des gesamten Graphen.
        /// </summary>
        /// <param name="layout1">Die alte Revision (t-1)</param>
        /// <param name="layout2">Die neue Revision (t)</param>
        /// <returns>Einen Wert >= 0 (z.B. 0.05 bedeutet 5% Größenänderung)</returns>
        public float CalculateRelativeWeightChange(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
        {
            float totalWeightPrevious = 0f;
            float totalAbsoluteChange = 0f;

            // Schritt 1: Schnellen Zugriff (O(1)) auf beide Layouts aufbauen
            Dictionary<string, NodeTransform> l1ById = new Dictionary<string, NodeTransform>();
            Dictionary<string, NodeTransform> l2ById = new Dictionary<string, NodeTransform>();

            // Ein HashSet sammelt alle existierenden IDs aus BEIDEN Revisionen (ohne Duplikate)
            HashSet<string> allUniqueNodeIds = new HashSet<string>();

            foreach (var kvp in layout1)
            {
                string id = kvp.Key.ID;
                l1ById[id] = kvp.Value;
                allUniqueNodeIds.Add(id);
            }

            foreach (var kvp in layout2)
            {
                string id = kvp.Key.ID;
                l2ById[id] = kvp.Value;
                allUniqueNodeIds.Add(id);
            }

            // Schritt 2: Über alle jemals existierenden Knoten iterieren
            foreach (string id in allUniqueNodeIds)
            {
                float weight1 = 0f;
                float weight2 = 0f;

                // Hatte der Knoten in Revision 1 ein Gewicht?
                if (l1ById.TryGetValue(id, out NodeTransform t1))
                {
                    // Hier definieren wir "Gewicht" als die 2D-Fläche (Breite * Tiefe)
                    weight1 = t1.Scale.x * t1.Scale.z;
                }

                // Hat der Knoten in Revision 2 ein Gewicht?
                if (l2ById.TryGetValue(id, out NodeTransform t2))
                {
                    weight2 = t2.Scale.x * t2.Scale.z;
                }

                // Für den Nenner der Formel: Gesamtes Ausgangsgewicht aufsummieren
                totalWeightPrevious += weight1;

                // Für den Zähler der Formel: Die absolute Größenänderung dieses Knotens
                totalAbsoluteChange += Mathf.Abs(weight2 - weight1);
            }

            // Schritt 3: Normalisierung (Division durch das alte Gesamtgewicht)
            // Verhindert Division durch Null, falls das Startlayout komplett leer war
            if (totalWeightPrevious <= 0f)
            {
                // Wenn das alte Layout leer war und im neuen Knoten sind, 
                // ist die relative Änderung eigentlich unendlich (oder 100%).
                // Wir geben hier einfach den absoluten Zuwachs oder 1.0f zurück.
                return totalAbsoluteChange > 0 ? 1.0f : 0f;
            }

            // RWC Formel: sum(|a(v,t) - a(v, t-1)|) / sum(a(v, t-1))
            return totalAbsoluteChange / totalWeightPrevious;
        }
        #endregion

        //*************************************************************************************************************
        #region mull
        public struct CircleData
        {
            public int Id;     // Wichtig: Eindeutige ID des Kreises zur Nachverfolgung
            public double X;
            public double Y;
            public double Radius;

            public CircleData(int id, double x, double y, double radius)
            {
                Id = id;
                X = x;
                Y = y;
                Radius = radius;
            }
        }

        /// <summary>
        /// 1. Kontaktgraphen-Erhalt (Contact Graph Overlap)
        /// Berechnet den Jaccard-Index der Kanten (Berührungen) zwischen zwei Layout-Zuständen.
        /// Ein Wert von 1.0 bedeutet perfekten Erhalt des Kontaktgraphen.
        /// </summary>
        /// <param name="layout1">Das Kreis-Layout vor der Transition</param>
        /// <param name="layout2">Das Kreis-Layout nach der Transition</param>
        /// <param name="tolerance">Räumliche Toleranz für "Berührung" (z.B. 0.001)</param>
        public static double CalculateContactGraphJaccard(List<CircleData> layout1, List<CircleData> layout2, double tolerance = 0.001)
        {
            // Kantenmengen extrahieren
            HashSet<string> edges1 = ExtractContactEdges(layout1, tolerance);
            HashSet<string> edges2 = ExtractContactEdges(layout2, tolerance);

            // Wenn es in beiden Layouts keine Kontakte gibt, ist die Topologie "perfekt" erhalten (leerer Graph)
            if (edges1.Count == 0 && edges2.Count == 0) return 1.0;

            // Jaccard-Index berechnen: |Intersection| / |Union|
            int intersectionCount = edges1.Intersect(edges2).Count();
            int unionCount = edges1.Union(edges2).Count();

            return (double)intersectionCount / unionCount;
        }

        /// <summary>
        /// 2. K-Nearest-Neighbor Jaccard-Index
        /// Berechnet den durchschnittlichen Erhalt der K nächsten Nachbarn für jeden Kreis.
        /// </summary>
        /// <param name="layout1">Das Kreis-Layout vor der Transition</param>
        /// <param name="layout2">Das Kreis-Layout nach der Transition</param>
        /// <param name="k">Anzahl der zu prüfenden Nachbarn</param>
        public static double CalculateKnnJaccard(List<CircleData> layout1, List<CircleData> layout2, int k)
        {
            Dictionary<int, HashSet<int>> knn1 = ExtractKnn(layout1, k);
            Dictionary<int, HashSet<int>> knn2 = ExtractKnn(layout2, k);

            double totalJaccard = 0;
            int evaluatedNodes = 0;

            foreach (var id in knn1.Keys)
            {
                // Prüfen, ob der Kreis auch im neuen Layout existiert (wichtig für den inkrementellen Aufbau)
                if (knn2.ContainsKey(id))
                {
                    HashSet<int> neighbors1 = knn1[id];
                    HashSet<int> neighbors2 = knn2[id];

                    int intersection = neighbors1.Intersect(neighbors2).Count();
                    int union = neighbors1.Union(neighbors2).Count();

                    if (union > 0)
                    {
                        totalJaccard += (double)intersection / union;
                    }
                    else
                    {
                        totalJaccard += 1.0; // Keine Nachbarn in beiden Fällen = perfekt erhalten
                    }

                    evaluatedNodes++;
                }
            }

            if (evaluatedNodes == 0) return 0;
            return totalJaccard / evaluatedNodes; // Globaler Durchschnitt
        }

        // --- INTERNE HILFSFUNKTIONEN ---

        // Extrahiert alle Kanten (Berührungen) eines Layouts als eindeutige Strings "MinId_MaxId"
        private static HashSet<string> ExtractContactEdges(List<CircleData> layout, double tolerance)
        {
            HashSet<string> edges = new HashSet<string>();

            for (int i = 0; i < layout.Count; i++)
            {
                for (int j = i + 1; j < layout.Count; j++)
                {
                    CircleData c1 = layout[i];
                    CircleData c2 = layout[j];

                    double dx = c1.X - c2.X;
                    double dy = c1.Y - c2.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    // Prüfen, ob sie sich berühren (mit Toleranz)
                    if (dist <= c1.Radius + c2.Radius + tolerance)
                    {
                        int minId = Math.Min(c1.Id, c2.Id);
                        int maxId = Math.Max(c1.Id, c2.Id);
                        edges.Add($"{minId}_{maxId}"); // Eindeutige Kanten-ID
                    }
                }
            }

            return edges;
        }

        // Extrahiert für jeden Kreis ein Set der IDs seiner k nächsten Nachbarn
        private static Dictionary<int, HashSet<int>> ExtractKnn(List<CircleData> layout, int k)
        {
            var knnDict = new Dictionary<int, HashSet<int>>();

            foreach (var circle in layout)
            {
                // Berechne Distanz zu allen ANDEREN Kreisen und sortiere aufsteigend
                var neighbors = layout
                    .Where(other => other.Id != circle.Id)
                    .OrderBy(other => GetDistance(circle, other))
                    .Take(k)
                    .Select(other => other.Id);

                knnDict[circle.Id] = new HashSet<int>(neighbors);
            }

            return knnDict;
        }

        private static double GetDistance(CircleData c1, CircleData c2)
        {
            double dx = c1.X - c2.X;
            double dy = c1.Y - c2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        //*************************************************************************************************************




        /*
        public void EvaluateRevisions()
        {
          LayoutMetricsEvaluator evaluator = new LayoutMetricsEvaluator();

          for (int i = 1; i < totalRevisions; i++)
          {
            var alt = history[i - 1];
            var neu = history[i];

            float rwc = evaluator.CalculateRelativeWeightChange(alt, neu);
            float ranking = evaluator.CalculateRanking(alt, neu);
            float adn = evaluator.CalculateADN(alt, neu);

            // Gebe die Daten kommasepariert in der Unity Console aus (CSV-Format)
            // Das kannst du dir direkt kopieren und in Excel/Python für die Plots einfügen!
            Debug.Log($"Revision {i}; RWC: {rwc:F4}; Ranking: {ranking:F4}; ADN: {adn:F4}");
          }
        }
         */

        [Test]
        public void JustTestEuclideanMentalDistance()
        {
            List<List<LayoutVertex>> layoutVertexGroups = new List<List<LayoutVertex>>();
            List<LayoutVertex> layoutVertices = new List<LayoutVertex>();

            List<Dictionary<ILayoutNode, NodeTransform>> incLayouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<IncrementalRectanglePackingLayout> incPackers = new List<IncrementalRectanglePackingLayout>();
            List<float> incEuclidianDists = new List<float>();
            float incEuclidianDist = 0f;


            List<Dictionary<ILayoutNode, NodeTransform>> layouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<RectanglePackingNodeLayout> packers = new List<RectanglePackingNodeLayout>();
            List<float> euclidianDists = new List<float>();
            float euclidianDist = 0f;


            for (int j = 0; j < 100; j++)
            {
                layoutVertices = new List<LayoutVertex>();
                for (int i = 0; i < Random.Range(1, 101); i++)
                {
                    layoutVertices.Add(new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), i));
                }
                layoutVertexGroups.Add(layoutVertices);
            }

            for (int i = 0; i < 100; i++)
            {
                incPackers.Add(new IncrementalRectanglePackingLayout());
                packers.Add(new RectanglePackingNodeLayout());

                if (i > 0)
                {
                    incPackers[i].oldLayout = incPackers[i - 1];
                }
            }

            for (int i = 0; i < 100; i++)
            {
                incLayouts.Add(incPackers[i].Create(layoutVertexGroups[i], Vector3.zero, Vector2.one));
                layouts.Add(packers[i].Create(layoutVertexGroups[i], Vector3.zero, Vector2.one));

                if (i > 0)
                {
                    // Calculate Euclidean mental distance or perform other operations
                    incEuclidianDists.Add(CalculateEuclideanMentalDistance(incLayouts[i], incLayouts[i - 1]));
                    euclidianDists.Add(CalculateEuclideanMentalDistance(layouts[i], layouts[i - 1]));

                    //incEuclidianDist += CalculateEuclideanMentalDistance(incLayouts[i], incLayouts[i - 1]);
                    //euclidianDist += CalculateEuclideanMentalDistance(layouts[i], layouts[i - 1]);
                }
            }



            Debug.Log("incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists));
            Debug.Log("Euclidean Distances: " + string.Join(", ", euclidianDists));

            //Debug.Log("incremental Euclidean Distances: " + incEuclidianDist);
            //Debug.Log("Euclidean Distances: " + euclidianDist);
        }

        [Test]
        public void JustTestADN()
        {
            List<List<LayoutVertex>> layoutVertexGroups = new List<List<LayoutVertex>>();
            List<LayoutVertex> layoutVertices = new List<LayoutVertex>();

            List<Dictionary<ILayoutNode, NodeTransform>> incLayouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<IncrementalRectanglePackingLayout> incPackers = new List<IncrementalRectanglePackingLayout>();
            List<float> incADNDists = new List<float>();
            float incADNDist = 0f;


            List<Dictionary<ILayoutNode, NodeTransform>> layouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<RectanglePackingNodeLayout> packers = new List<RectanglePackingNodeLayout>();
            List<float> ADNDists = new List<float>();
            float ADNDist = 0f;


            for (int j = 0; j < 100; j++)
            {
                layoutVertices = new List<LayoutVertex>();
                for (int i = 0; i < Random.Range(1, 101); i++)
                {
                    layoutVertices.Add(new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), i));
                }
                layoutVertexGroups.Add(layoutVertices);
            }

            for (int i = 0; i < 100; i++)
            {
                incPackers.Add(new IncrementalRectanglePackingLayout());
                packers.Add(new RectanglePackingNodeLayout());

                if (i > 0)
                {
                    incPackers[i].oldLayout = incPackers[i - 1];
                }
            }

            for (int i = 0; i < 100; i++)
            {
                incLayouts.Add(incPackers[i].Create(layoutVertexGroups[i], Vector3.zero, Vector2.one));
                layouts.Add(packers[i].Create(layoutVertexGroups[i], Vector3.zero, Vector2.one));

                if (i > 0)
                {
                    // Calculate ADN distance or perform other operations
                    incADNDists.Add(CalculateADN(incLayouts[i], incLayouts[i - 1]));
                    ADNDists.Add(CalculateADN(layouts[i], layouts[i - 1]));

                    //incADNDist += CalculateADN(incLayouts[i], incLayouts[i - 1]);
                    //ADNDist += CalculateADN(layouts[i], layouts[i - 1]);
                }
            }



            Debug.Log("incremental ADN Distances: " + string.Join(", ", incADNDists));
            Debug.Log("ADN Distances: " + string.Join(", ", ADNDists));

            //Debug.Log("incremental ADN Distances: " + incADNDist);
            //Debug.Log("ADN Distances: " + ADNDist);
        }


        //***************************************************************************************************************
        #region make a graph

        [Serializable]
        public class VertexData
        {
            public Vector3 size;
            public string id;

            public VertexData(Vector3 size, string id)
            {
                this.size = size;
                this.id = id;
            }
        }

        [Serializable]
        public class VertexGroupData
        {
            public List<VertexData> vertices = new List<VertexData>();
        }

        [Serializable]
        public class EvaluationGraphData
        {
            public List<VertexGroupData> groups = new List<VertexGroupData>();
        }
        private string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, "EvaluationGraph1.json");
        }

        private string GetFilePath1()
        {
            return Path.Combine(Application.persistentDataPath, "data.xlsx");
        }

        /// <summary>
        /// Saves the flat list of vertices to JSON.
        /// </summary>
        public void SaveGraph(List<List<LayoutVertex>> layoutVertexGroups)
        {
            EvaluationGraphData graphData = new EvaluationGraphData();

            foreach (List<LayoutVertex> group in layoutVertexGroups)
            {
                VertexGroupData newGroupData = new VertexGroupData();

                foreach (LayoutVertex vertex in group)
                {
                    // Assuming vertex.size exists. If roots/parents don't have a size, 
                    // it will safely default to Vector3.zero
                    newGroupData.vertices.Add(new VertexData(vertex.AbsoluteScale, vertex.ID));
                }

                graphData.groups.Add(newGroupData);
            }

            string json = JsonUtility.ToJson(graphData, true);
            File.WriteAllText(GetFilePath(), json);

            Debug.Log($"Successfully saved hierarchical graph to: {GetFilePath()}");
        }

        public List<List<LayoutVertex>> LoadGraph()
        {
            string path = GetFilePath();

            if (!File.Exists(path))
            {
                Debug.LogWarning("EvaluationGraph.json not found.");
                return new List<List<LayoutVertex>>();
            }

            string json = File.ReadAllText(path);
            EvaluationGraphData graphData = JsonUtility.FromJson<EvaluationGraphData>(json);
            List<List<LayoutVertex>> recreatedGroups = new List<List<LayoutVertex>>();

            if (graphData != null && graphData.groups != null)
            {
                foreach (VertexGroupData groupData in graphData.groups)
                {
                    List<LayoutVertex> newGroup = new List<LayoutVertex>();

                    // We use a dictionary to easily find vertices by their ID string
                    Dictionary<string, LayoutVertex> vertexLookup = new Dictionary<string, LayoutVertex>();

                    // STEP 1: Recreate all instances based on their IDs
                    foreach (VertexData vData in groupData.vertices)
                    {
                        LayoutVertex newVertex;

                        // Figure out which constructor to use. 
                        // Children have "ppp" in their ID, roots and parents don't.
                        if (vData.id.Contains("ppp"))
                        {
                            newVertex = new LayoutVertex(vData.size, vData.id);
                        }
                        else
                        {
                            newVertex = new LayoutVertex(vData.id);
                        }

                        newGroup.Add(newVertex);
                        vertexLookup.Add(vData.id, newVertex);
                    }

                    // STEP 2: Re-establish the AddChild() relationships
                    foreach (LayoutVertex vertex in newGroup)
                    {
                        // Find the parent's ID by taking everything before the last comma
                        // e.g., "p,pp0,ppp1" becomes "p,pp0"
                        int lastCommaIndex = vertex.ID.LastIndexOf(',');

                        if (lastCommaIndex != -1) // If it HAS a comma, it has a parent
                        {
                            string parentId = vertex.ID.Substring(0, lastCommaIndex);

                            // Find the parent in our dictionary and add this vertex as a child
                            if (vertexLookup.TryGetValue(parentId, out LayoutVertex parentVertex))
                            {
                                parentVertex.AddChild(vertex);
                            }
                        }
                    }

                    recreatedGroups.Add(newGroup);
                }
            }

            Debug.Log($"Successfully recreated {recreatedGroups.Count} layout groups with restored hierarchies.");
            return recreatedGroups;
        }


        [Test]
        public void JustMakeGraph()
        {
            List<List<LayoutVertex>> newlayoutVertexGroups = new List<List<LayoutVertex>>();
            List<LayoutVertex> newlayoutVertices = new List<LayoutVertex>();

            for (int k = 0; k < 100; k++)
            {
                newlayoutVertices = new List<LayoutVertex>();
                LayoutVertex root = new LayoutVertex("p");
                newlayoutVertices.Add(root);

                for (int j = 0; j < Random.Range(1, 101); j++)
                {
                    LayoutVertex parentVertex = new LayoutVertex("p" + ",pp" + j);
                    root.AddChild(parentVertex);
                    newlayoutVertices.Add(parentVertex);
                    for (int i = 0; i < Random.Range(1, 101); i++)
                    {
                        string cid = "p" + ",pp" + j + ",ppp" + i;
                        LayoutVertex child = new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), cid);
                        parentVertex.AddChild(child);
                        newlayoutVertices.Add(child);
                    }

                }

                newlayoutVertexGroups.Add(newlayoutVertices);

            }

            SaveGraph(newlayoutVertexGroups);

        }
        public List<List<LayoutVertex>> JustMakeRandomGraphWithoutSave()
        {
            List<List<LayoutVertex>> newlayoutVertexGroups = new List<List<LayoutVertex>>();
            List<LayoutVertex> newlayoutVertices = new List<LayoutVertex>();

            for (int k = 0; k < 100; k++)
            {
                newlayoutVertices = new List<LayoutVertex>();
                LayoutVertex root = new LayoutVertex("p");
                newlayoutVertices.Add(root);

                for (int j = 0; j < Random.Range(0, 101); j++)
                {
                    LayoutVertex parentVertex = new LayoutVertex("p" + ",pp" + j);
                    root.AddChild(parentVertex);
                    newlayoutVertices.Add(parentVertex);
                    for (int i = 0; i < Random.Range(0, 101); i++)
                    {
                        string cid = "p" + ",pp" + j + ",ppp" + i;
                        LayoutVertex child = new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), cid);
                        parentVertex.AddChild(child);
                        newlayoutVertices.Add(child);
                    }

                }

                newlayoutVertexGroups.Add(newlayoutVertices);

            }
            return newlayoutVertexGroups;
        }

        public void ExportGraphsToGXL(List<List<LayoutVertex>> layoutVertexGroups, string directoryName = "GXL_Exports")
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, directoryName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            XNamespace xlink = "http://www.w3.org/1999/xlink";

            for (int k = 0; k < layoutVertexGroups.Count; k++)
            {
                List<LayoutVertex> currentGraph = layoutVertexGroups[k];

                // WICHTIG: k fängt bei 0 an, wir wollen aber bei 1 anfangen zu zählen
                int index = k + 1;
                string fileName = "clones-" + index;

                // GXL und Graph Root-Elemente erstellen (id auch direkt auf clones-X gesetzt)
                XElement gxlRoot = new XElement("gxl", new XAttribute(XNamespace.Xmlns + "xlink", xlink));
                XElement graphElement = new XElement("graph",
                    new XAttribute("id", fileName),
                    new XAttribute("edgeids", "true")
                );
                gxlRoot.Add(graphElement);

                int edgeCounter = 1;

                foreach (LayoutVertex vertex in currentGraph)
                {
                    string vId = vertex.ID;
                    int level = vId.Split(',').Length - 1;
                    string typeName = (level == 2) ? "File" : "Directory";
                    string sourceName = (level > 0) ? vId.Substring(vId.LastIndexOf(',') + 1) : vId;

                    XElement nodeElement = new XElement("node", new XAttribute("id", vId));
                    nodeElement.Add(new XElement("type", new XAttribute(xlink + "href", typeName)));

                    nodeElement.Add(CreateStringAttr("Source.Name", sourceName));
                    nodeElement.Add(CreateStringAttr("Linkage.Name", vId));
                    nodeElement.Add(CreateIntAttr("Level", level));

                    if (level == 2)
                    {
                        nodeElement.Add(CreateFloatAttr("Metric.X", vertex.AbsoluteScale.x));
                        nodeElement.Add(CreateFloatAttr("Metric.Y", vertex.AbsoluteScale.y));
                        nodeElement.Add(CreateFloatAttr("Metric.Z", vertex.AbsoluteScale.z));
                    }
                    else
                    {
                        nodeElement.Add(CreateIntAttr("Metric.Number_Of_Descendants", currentGraph.Count));
                    }

                    graphElement.Add(nodeElement);

                    if (level > 0)
                    {
                        string parentId = vId.Substring(0, vId.LastIndexOf(','));

                        XElement edgeElement = new XElement("edge",
                            new XAttribute("id", "E" + edgeCounter),
                            new XAttribute("from", vId),
                            new XAttribute("to", parentId)
                        );

                        edgeElement.Add(new XElement("type", new XAttribute(xlink + "href", "Belongs_To")));
                        graphElement.Add(edgeElement);

                        edgeCounter++;
                    }
                }

                XDocument doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XDocumentType("gxl", null, "http://www.gupro.de/GXL/gxl-1.0.dtd", null),
                    gxlRoot
                );

                // HIER WIRD DER NEUE DATEINAME VERWENDET: clones-1.gxl, clones-2.gxl, etc.
                string filePath = Path.Combine(directoryPath, fileName + ".gxl");
                doc.Save(filePath);
            }

            Debug.Log($"Erfolgreich {layoutVertexGroups.Count} GXL Dateien exportiert nach:\n{directoryPath}");


            // --- HILFSFUNKTIONEN (unverändert) ---
            XElement CreateStringAttr(string name, string value)
            {
                return new XElement("attr", new XAttribute("name", name), new XElement("string", value));
            }

            XElement CreateIntAttr(string name, int value)
            {
                return new XElement("attr", new XAttribute("name", name), new XElement("int", value));
            }

            XElement CreateFloatAttr(string name, float value)
            {
                return new XElement("attr", new XAttribute("name", name),
                    new XElement("float", value.ToString("F6", CultureInfo.InvariantCulture))
                );
            }
        }


        [Test]
        public void JustTestExportToGXL()
        {
            List<List<LayoutVertex>> layoutVertexGroups = JustMakeRandomGraphWithoutSave();
            ExportGraphsToGXL(layoutVertexGroups);
        }

        public List<List<LayoutVertex>> ImportGXLToGraphs(string directoryName = "GXL_Exports")
        {
            List<List<LayoutVertex>> layoutVertexGroups = new List<List<LayoutVertex>>();
            string directoryPath = Path.Combine(Application.persistentDataPath, directoryName);

            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning("Verzeichnis nicht gefunden: " + directoryPath);
                return layoutVertexGroups;
            }

            // 1. Alle GXL-Dateien holen
            string[] filePaths = Directory.GetFiles(directoryPath, "*.gxl");

            // 2. Dateien nach der Nummer im Namen sortieren (clones-1, clones-2, ... clones-10)
            var sortedFiles = filePaths.OrderBy(f =>
            {
                string fileName = Path.GetFileNameWithoutExtension(f); // Gibt z.B. "clones-1" zurück
                string numberString = fileName.Replace("clones-", ""); // Gibt "1" zurück

                if (int.TryParse(numberString, out int number))
                    return number;

                return 0; // Fallback, falls der Name unerwartet formatiert ist
            }).ToArray();

            // 3. Jede Datei parsen
            foreach (string file in sortedFiles)
            {
                List<LayoutVertex> currentGraph = new List<LayoutVertex>();

                // Ein Dictionary hilft uns, die Knoten später für die Kanten schnell zu finden
                Dictionary<string, LayoutVertex> vertexLookup = new Dictionary<string, LayoutVertex>();

                XDocument doc = XDocument.Load(file);

                // --- KNOTEN (NODES) AUSLESEN ---
                foreach (XElement node in doc.Descendants("node"))
                {
                    string vId = node.Attribute("id")?.Value;

                    float x = 0f, y = 0f, z = 0f;
                    bool isFile = false;

                    // Attribute durchsuchen
                    foreach (XElement attr in node.Elements("attr"))
                    {
                        string attrName = attr.Attribute("name")?.Value;

                        if (attrName == "Metric.X")
                        {
                            x = ParseFloat(attr.Element("float")?.Value);
                            isFile = true; // Nur Files (Level 2) haben Koordinaten
                        }
                        else if (attrName == "Metric.Y")
                        {
                            y = ParseFloat(attr.Element("float")?.Value);
                        }
                        else if (attrName == "Metric.Z")
                        {
                            z = ParseFloat(attr.Element("float")?.Value);
                        }
                    }

                    LayoutVertex newVertex;

                    if (isFile)
                    {
                        // Erstelle ein Leaf/Child mit Koordinaten
                        newVertex = new LayoutVertex(new Vector3(x, y, z), vId);
                    }
                    else
                    {
                        // Erstelle Root oder Parent (ohne Koordinaten)
                        newVertex = new LayoutVertex(vId);
                    }

                    currentGraph.Add(newVertex);
                    vertexLookup[vId] = newVertex;
                }

                // --- KANTEN (EDGES) AUSLESEN UND HIERARCHIE AUFBAUEN ---
                foreach (XElement edge in doc.Descendants("edge"))
                {
                    // In unserem Export ist "from" das Kind (N1) und "to" der Vater (N)
                    string childId = edge.Attribute("from")?.Value;
                    string parentId = edge.Attribute("to")?.Value;

                    if (!string.IsNullOrEmpty(childId) && !string.IsNullOrEmpty(parentId))
                    {
                        // Finde beide Knoten im Dictionary
                        if (vertexLookup.TryGetValue(parentId, out LayoutVertex parentNode) &&
                            vertexLookup.TryGetValue(childId, out LayoutVertex childNode))
                        {
                            // Stelle die Eltern-Kind-Beziehung wieder her
                            parentNode.AddChild(childNode);
                        }
                    }
                }

                layoutVertexGroups.Add(currentGraph);
            }

            // --- HILFSFUNKTION ---
            // Liest den Float-Wert sicher aus und ignoriert regionale Einstellungen (Punkte vs. Kommas)
            float ParseFloat(string valueStr)
            {
                if (string.IsNullOrEmpty(valueStr)) return 0f;

                if (float.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
                return 0f;
            }
            Debug.Log($"Erfolgreich {layoutVertexGroups.Count} GXL Dateien geladen und Graphen rekonstruiert aus:\n{directoryPath}");
            return layoutVertexGroups;
        }

        [Test]
        public void JustTestImportFromGXL()
        {
            List<List<LayoutVertex>> importedGraphs = ImportGXLToGraphs("GXL_Exports");
            // Optional: Überprüfe die Struktur des ersten importierten Graphen
            if (importedGraphs.Count > 0)
            {
                List<LayoutVertex> firstGraph = importedGraphs[0];
                Debug.Log($"Erster importierter Graph hat {firstGraph.Count} Knoten. Beispielknoten: {firstGraph[0].ID}, Size: {firstGraph[0].AbsoluteScale}");
            }
        }

        //***************************************************************************************************************

        public List<List<LayoutVertex>> ImportSEEClones(string directoryPath)
        {
            List<List<LayoutVertex>> layoutVertexGroups = new List<List<LayoutVertex>>();

            // --- CHANGED HERE: Point to the Extracted_GXL subfolder ---
            string targetDirectory = Path.Combine(directoryPath);

            if (!Directory.Exists(targetDirectory))
            {
                Debug.LogError($"Directory not found: {targetDirectory}. Did you run the batch script?");
                return layoutVertexGroups;
            }

            // 1. Get all standard .gxl files
            string[] filePaths = Directory.GetFiles(targetDirectory, "*.gxl");

            // 2. Sort files numerically (clones-1, clones-2 ... clones-102)
            var sortedFiles = filePaths.OrderBy(f =>
            {
                string fileName = Path.GetFileNameWithoutExtension(f); // e.g. "clones-1"
                string numberString = fileName.Replace("clones-", ""); // e.g. "1"

                if (int.TryParse(numberString, out int number))
                    return number;

                return 0;
            }).ToArray();

            // Need the xlink namespace to read <type xlink:href="File"/>
            XNamespace xlink = "http://www.w3.org/1999/xlink";

            // 3. Parse each file
            foreach (string file in sortedFiles)
            {
                List<LayoutVertex> currentGraph = new List<LayoutVertex>();
                Dictionary<string, LayoutVertex> vertexLookup = new Dictionary<string, LayoutVertex>();

                XDocument doc = XDocument.Load(file);

                // --- STEP A: READ NODES ---
                foreach (XElement node in doc.Descendants("node"))
                {
                    string vId = node.Attribute("id")?.Value;

                    // Read node type (File vs Directory)
                    string nodeType = node.Element("type")?.Attribute(xlink + "href")?.Value;

                    int tokens = 0;
                    int loc = 0;

                    // Read attributes
                    foreach (XElement attr in node.Elements("attr"))
                    {
                        string attrName = attr.Attribute("name")?.Value;

                        if (attrName == "Metric.Number_of_Tokens")
                        {
                            int.TryParse(attr.Element("int")?.Value, out tokens);
                        }
                        else if (attrName == "Metric.LOC")
                        {
                            int.TryParse(attr.Element("int")?.Value, out loc);
                        }
                    }

                    LayoutVertex newVertex;

                    if (nodeType == "File")
                    {
                        // As requested: X = Tokens, Y = LOC, Z = Tokens
                        Vector3 size = new Vector3(tokens, loc, tokens);
                        newVertex = new LayoutVertex(size, vId);
                    }
                    else
                    {
                        // It's a Directory, no spatial metrics needed
                        newVertex = new LayoutVertex(vId);
                    }

                    currentGraph.Add(newVertex);
                    vertexLookup[vId] = newVertex;
                }

                // --- STEP B: READ EDGES & REBUILD HIERARCHY ---
                foreach (XElement edge in doc.Descendants("edge"))
                {
                    // In your GXL: <edge from="N1" to="N2"> 
                    // N1 (File) is enclosed by N2 (Directory). 
                    // So "from" is the child, "to" is the parent.
                    string childId = edge.Attribute("from")?.Value;
                    string parentId = edge.Attribute("to")?.Value;

                    if (!string.IsNullOrEmpty(childId) && !string.IsNullOrEmpty(parentId))
                    {
                        if (vertexLookup.TryGetValue(parentId, out LayoutVertex parentNode) &&
                            vertexLookup.TryGetValue(childId, out LayoutVertex childNode))
                        {
                            parentNode.AddChild(childNode);
                        }
                    }
                }

                layoutVertexGroups.Add(currentGraph);
            }

            Debug.Log($"Successfully loaded {layoutVertexGroups.Count} GXL graphs from:\n{targetDirectory}");
            return layoutVertexGroups;
        }
        // A reference to your loaded graphs


        [Test]
        public void JustTestImportAnimation_Clones_SEE()
        {
            List<List<LayoutVertex>> animationFrames = new List<List<LayoutVertex>>();
            // 1. Define the path to your folder.
            // For testing, you can use a direct absolute path like:
            // string folderPath = @"C:\Users\YourName\Desktop\animation-clones-SEE";

            // Alternatively, put the folder in your Unity project's "StreamingAssets" folder:
            string folderPath = Path.Combine(Application.persistentDataPath, "Extracted_GXL");

            // 2. Create the importer

            // 3. Load the graphs
            animationFrames = ImportSEEClones(Path.Combine(Application.persistentDataPath, "Extracted_GXL"));

            // 4. Verify it worked
            if (animationFrames.Count > 0)
            {
                Debug.Log($"Loaded {animationFrames.Count} frames.");

                // Example: Look at the first graph
                List<LayoutVertex> firstGraph = animationFrames[0];
                Debug.Log($"The first graph has {firstGraph.Count} vertices.");
            }
        }


        #endregion

        #region analyze kurven
        /// <summary>
        /// Berechnet den Mean Absolute Error (MAE).
        /// Zeigt die durchschnittliche absolute Abweichung pro Transition.
        /// </summary>
        public double CalculateMAE(List<double> curveA, List<double> curveB)
        {
            if (curveA.Count != curveB.Count)
                throw new ArgumentException("Die Arrays müssen gleich lang sein.");

            double sum = 0;
            for (int i = 0; i < curveA.Count; i++)
            {
                sum += Math.Abs(curveA[i] - curveB[i]);
            }
            return sum / curveA.Count;
        }

        /// <summary>
        /// Berechnet den Root Mean Squared Error (RMSE).
        /// Bestraft große Abweichungen (Ausreißer) stärker als der MAE.
        /// </summary>
        public double CalculateRMSE(List<double> curveA, List<double> curveB)
        {
            if (curveA.Count != curveB.Count)
                throw new ArgumentException("Die Arrays müssen gleich lang sein.");

            double sumSq = 0;
            for (int i = 0; i < curveA.Count; i++)
            {
                double diff = curveA[i] - curveB[i];
                sumSq += diff * diff;
            }
            return Math.Sqrt(sumSq / curveA.Count);
        }

        /// <summary>
        /// Berechnet den Pearson-Korrelationskoeffizienten (r).
        /// Wertebereich: -1.0 bis 1.0. Zeigt die Trend-Ähnlichkeit der Kurven.
        /// </summary>
        public double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count)
                throw new ArgumentException("Die Arrays müssen gleich lang sein.");

            int n = x.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumX2 += x[i] * x[i];
                sumY2 += y[i] * y[i];
            }

            double numerator = (n * sumXY) - (sumX * sumY);
            double denominator = Math.Sqrt(((n * sumX2) - (sumX * sumX)) * ((n * sumY2) - (sumY * sumY)));

            // Division durch Null abfangen (passiert, wenn eine Kurve komplett flach ist)
            if (denominator == 0) return 0;

            return numerator / denominator;
        }

        /// <summary>
        /// Berechnet die Fläche unter der Kurve (Area Under Curve - AUC) 
        /// mittels Trapezregel. Nimmt an, dass der X-Abstand (Transition) immer 1 ist.
        /// </summary>
        public double CalculateAUC(List<double> curve)
        {
            if (curve.Count < 2) return 0;

            double area = 0;
            for (int i = 0; i < curve.Count - 1; i++)
            {
                // Die Fläche des Trapezes zwischen Punkt i und Punkt i+1
                area += (curve[i] + curve[i + 1]) / 2.0;
            }
            return area;
        }

        /// <summary>
        /// Berechnet die Varianz (Populationsvarianz) einer einzelnen Kurve.
        /// Zeigt, wie stark die Werte um ihren eigenen Durchschnitt schwanken (Volatilität).
        /// </summary>
        public double CalculateVariance(List<double> curve)
        {
            if (curve.Count == 0) return 0;

            double mean = curve.Average();
            double sumOfSquares = 0;

            foreach (double val in curve)
            {
                double diff = val - mean;
                sumOfSquares += diff * diff;
            }

            return sumOfSquares / curve.Count;
        }

        /// <summary>
        /// 1. Variationskoeffizient (Coefficient of Variation - CV)
        /// Misst die relative Streuung. Ein Wert von 0.5 bedeutet, dass die Kurve 
        /// im Durchschnitt um 50% ihres eigenen Niveaus schwankt.
        /// </summary>
        public double CalculateCoefficientOfVariation(double[] curve)
        {
            if (curve.Length == 0) return 0;

            double mean = curve.Average();
            if (mean == 0) return 0; // Verhindert Division durch Null

            double sumOfSquares = 0;
            foreach (double val in curve)
            {
                sumOfSquares += (val - mean) * (val - mean);
            }

            double variance = sumOfSquares / curve.Length;
            double stdDev = Math.Sqrt(variance);

            return stdDev / mean;
        }

        /// <summary>
        /// 2. Peak-Analyse (Ausreißer-Häufigkeit)
        /// Zählt, wie oft ein lokales Maximum (Spike) auftritt, das einen 
        /// von dir definierten Schwellenwert (threshold) überschreitet.
        /// </summary>
        public int CountPeaks(double[] curve, double threshold)
        {
            if (curve.Length < 3) return 0; // Ein Peak braucht mindestens 3 Punkte (vorher, peak, nachher)

            int peakCount = 0;

            for (int i = 1; i < curve.Length - 1; i++)
            {
                // Prüfen, ob es ein lokales Maximum ist (höher als der Punkt links und rechts)
                if (curve[i] > curve[i - 1] && curve[i] > curve[i + 1])
                {
                    // Prüfen, ob dieser Spike den Schwellenwert überschreitet
                    if (curve[i] > threshold)
                    {
                        peakCount++;
                    }
                }
            }

            return peakCount;
        }

        /// <summary>
        /// 3. Approximative Entropie (ApEn)
        /// Misst das "Chaos" oder die Vorhersagbarkeit der Kurve. 
        /// Hohe Werte = Chaotisch/Rauschen. Niedrige Werte = Regelmäßig/Vorhersagbar.
        /// m: Länge der zu vergleichenden Muster (Standard: 2)
        /// rMultiplier: Toleranzgrenze bezogen auf die Standardabweichung (Standard: 0.2)
        /// </summary>
        public double CalculateApproximateEntropy(double[] curve, int m = 2, double rMultiplier = 0.2)
        {
            if (curve.Length < m + 1) return 0;

            // Schritt 1: Standardabweichung berechnen, um Toleranz 'r' zu definieren
            double mean = curve.Average();
            double variance = curve.Select(val => (val - mean) * (val - mean)).Sum() / curve.Length;
            double stdDev = Math.Sqrt(variance);
            double r = rMultiplier * stdDev;

            double Phi(double[] data, int m, double r)
            {
                int n = data.Length;
                int n_m = n - m + 1; // Anzahl der Muster der Länge m

                if (n_m <= 0) return 0;

                double sum = 0;

                // Vergleiche jedes Muster der Länge m mit jedem anderen Muster
                for (int i = 0; i < n_m; i++)
                {
                    int matchCount = 0;
                    for (int j = 0; j < n_m; j++)
                    {
                        bool isMatch = true;
                        // Prüfen, ob der Abstand an allen Stellen <= r ist (Chebyshev-Distanz)
                        for (int k = 0; k < m; k++)
                        {
                            if (Math.Abs(data[i + k] - data[j + k]) > r)
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch) matchCount++;
                    }

                    // Logarithmus der Wahrscheinlichkeit, dass Muster i ein Match findet
                    sum += Math.Log((double)matchCount / n_m);
                }

                return sum / n_m;
            }

            // Schritt 2: ApEn = Phi(m) - Phi(m+1)
            return Phi(curve, m, r) - Phi(curve, m + 1, r);
        }
        #endregion


        //*************************************************************************************************************


        [Test]
        public void JustTestLayoutIRPANDRP()
        {
            double totalTimeInMilliSecondsIncremental = 0;
            double totalTimeInMilliSeconds = 0;

            List<List<LayoutVertex>> newlayoutVertexGroups = new List<List<LayoutVertex>>();


            List<Dictionary<ILayoutNode, NodeTransform>> incLayouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<IncrementalRectanglePackingLayout> incPackers = new List<IncrementalRectanglePackingLayout>();
            List<float> incEuclidianDists = new List<float>();
            List<float> incADNDists = new List<float>();
            List<float> incAverageRelativeDistance = new List<float>();
            List<float> incLayoutDistanceChange = new List<float>();
            List<float> incNearestNeighborWithin = new List<float>();
            List<float> incRanking = new List<float>();
            List<float> incSERC = new List<float>();
            List<float> incRelativeWeightChange = new List<float>();

            List<Dictionary<ILayoutNode, NodeTransform>> layouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<RectanglePackingNodeLayout> packers = new List<RectanglePackingNodeLayout>();
            List<float> euclidianDists = new List<float>();
            List<float> ADNDists = new List<float>();
            List<float> averageRelativeDistance = new List<float>();
            List<float> layoutDistanceChange = new List<float>();
            List<float> nearestNeighborWithin = new List<float>();
            List<float> ranking = new List<float>();
            List<float> sERC = new List<float>();
            List<float> relativeWeightChange = new List<float>();

            double MAE1 = 0;
            double RMSE1 = 0;
            double PearsonCorrelation1 = 0;
            double incAUC1 = 0;
            double aUC1 = 0;
            double incVariance1 = 0;
            double variance1 = 0;

            double MAE2 = 0;
            double RMSE2 = 0;
            double PearsonCorrelation2 = 0;
            double incAUC2 = 0;
            double aUC2 = 0;
            double incVariance2 = 0;
            double variance2 = 0;

            double MAE3 = 0;
            double RMSE3 = 0;
            double PearsonCorrelation3 = 0;
            double incAUC3 = 0;
            double aUC3 = 0;
            double incVariance3 = 0;
            double variance3 = 0;

            double MAE4 = 0;
            double RMSE4 = 0;
            double PearsonCorrelation4 = 0;
            double incAUC4 = 0;
            double aUC4 = 0;
            double incVariance4 = 0;
            double variance4 = 0;

            double MAE5 = 0;
            double RMSE5 = 0;
            double PearsonCorrelation5 = 0;
            double incAUC5 = 0;
            double aUC5 = 0;
            double incVariance5 = 0;
            double variance5 = 0;

            double MAE6 = 0;
            double RMSE6 = 0;
            double PearsonCorrelation6 = 0;
            double incAUC6 = 0;
            double aUC6 = 0;
            double incVariance6 = 0;
            double variance6 = 0;

            double MAE7 = 0;
            double RMSE7 = 0;
            double PearsonCorrelation7 = 0;
            double incAUC7 = 0;
            double aUC7 = 0;
            double incVariance7 = 0;
            double variance7 = 0;

            double MAE8 = 0;
            double RMSE8 = 0;
            double PearsonCorrelation8 = 0;
            double incAUC8 = 0;
            double aUC8 = 0;
            double incVariance8 = 0;
            double variance8 = 0;

            /*
            for (int j = 0; j < 100; j++)
            {
              layoutVertices = new List<LayoutVertex>();
              for (int i = 0; i < Random.Range(1, 101); i++)
              {
                layoutVertices.Add(new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), i));
              }
              layoutVertexGroups.Add(layoutVertices);
            }
             */
            /*
            for (int j = 0; j < layoutVertices.Count; j++)
            {
              new LayoutVertex("pp" + j);
              layoutVertices = new List<LayoutVertex>();
              for (int i = 0; i < Random.Range(1, 101); i++)
              {
                layoutVertices.Add(new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), i));
              }
              listlayoutVertexGroups.Add(new List<List<LayoutVertex>>(layoutVertexGroups));
            }
             */

            /*
            for (int k = 0; k < 100; k++)
            {
              newlayoutVertices = new List<LayoutVertex>();
              LayoutVertex root = new LayoutVertex("p");
              newlayoutVertices.Add(root);

              for (int j = 0; j < 10; j++)
              {
                LayoutVertex parentVertex = new LayoutVertex("p" + ",pp" + j);
                root.AddChild(parentVertex);
                newlayoutVertices.Add(parentVertex);
                for (int i = 0; i < Random.Range(1, 101); i++)
                {
                  string cid = "p" + ",pp" + j + ",ppp" + i;
                  LayoutVertex child = new LayoutVertex(new Vector3(Random.Range(0f, 1f), 0.1f, Random.Range(0f, 1f)), cid);
                  parentVertex.AddChild(child);
                  newlayoutVertices.Add(child);
                }

              }

              newlayoutVertexGroups.Add(newlayoutVertices);

            }
             */

            //newlayoutVertexGroups = JustMakeRandomGraphWithoutSave();
            newlayoutVertexGroups = LoadGraph();
            //newlayoutVertexGroups = ImportGXLToGraphs();
            //newlayoutVertexGroups = ImportSEEClones(Path.Combine(Application.persistentDataPath, "Extracted_GXL"));



            for (int i = 0; i < 100; i++)
            {
                incPackers.Add(new IncrementalRectanglePackingLayout());
                packers.Add(new RectanglePackingNodeLayout());

                if (i > 0)
                {
                    incPackers[i].oldLayout = incPackers[i - 1];
                }
            }

            for (int i = 0; i < 100; i++)
            {
                Performance p = Performance.Begin("incremental Layout Evaluation");
                incLayouts.Add(incPackers[i].Create(newlayoutVertexGroups[i], Vector3.zero, Vector2.one));
                p.End();
                totalTimeInMilliSecondsIncremental += p.GetTimeInMilliSeconds();

                Performance p2 = Performance.Begin("Layout Evaluation");
                layouts.Add(packers[i].Create(newlayoutVertexGroups[i], Vector3.zero, Vector2.one));
                p2.End();
                totalTimeInMilliSeconds += p2.GetTimeInMilliSeconds();

                if (i > 0)
                {
                    incEuclidianDists.Add(CalculateEuclideanMentalDistance(incLayouts[i], incLayouts[i - 1]));
                    euclidianDists.Add(CalculateEuclideanMentalDistance(layouts[i], layouts[i - 1]));
                    MAE1 = CalculateMAE(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    RMSE1 = CalculateRMSE(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    PearsonCorrelation1 = CalculatePearsonCorrelation(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    incAUC1 = CalculateAUC(incEuclidianDists.Select(x => (double)x).ToList());
                    aUC1 = CalculateAUC(euclidianDists.Select(x => (double)x).ToList());
                    incVariance1 = CalculateVariance(incEuclidianDists.Select(x => (double)x).ToList());
                    variance1 = CalculateVariance(euclidianDists.Select(x => (double)x).ToList());

                    incADNDists.Add(CalculateADN(incLayouts[i], incLayouts[i - 1]));
                    ADNDists.Add(CalculateADN(layouts[i], layouts[i - 1]));
                    MAE2 = CalculateMAE(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    RMSE2 = CalculateRMSE(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    PearsonCorrelation2 = CalculatePearsonCorrelation(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    incAUC2 = CalculateAUC(incADNDists.Select(x => (double)x).ToList());
                    aUC2 = CalculateAUC(ADNDists.Select(x => (double)x).ToList());
                    incVariance2 = CalculateVariance(incADNDists.Select(x => (double)x).ToList());
                    variance2 = CalculateVariance(ADNDists.Select(x => (double)x).ToList());

                    incAverageRelativeDistance.Add(CalculateAverageRelativeDistance(incLayouts[i], incLayouts[i - 1]));
                    averageRelativeDistance.Add(CalculateAverageRelativeDistance(layouts[i], layouts[i - 1]));
                    MAE3 = CalculateMAE(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    RMSE3 = CalculateRMSE(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    PearsonCorrelation3 = CalculatePearsonCorrelation(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    incAUC3 = CalculateAUC(incAverageRelativeDistance.Select(x => (double)x).ToList());
                    aUC3 = CalculateAUC(averageRelativeDistance.Select(x => (double)x).ToList());
                    incVariance3 = CalculateVariance(incAverageRelativeDistance.Select(x => (double)x).ToList());
                    variance3 = CalculateVariance(averageRelativeDistance.Select(x => (double)x).ToList());

                    incLayoutDistanceChange.Add(CalculateLayoutDistanceChange(incLayouts[i], incLayouts[i - 1]));
                    layoutDistanceChange.Add(CalculateLayoutDistanceChange(layouts[i], layouts[i - 1]));
                    MAE4 = CalculateMAE(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    RMSE4 = CalculateRMSE(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    PearsonCorrelation4 = CalculatePearsonCorrelation(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    incAUC4 = CalculateAUC(incLayoutDistanceChange.Select(x => (double)x).ToList());
                    aUC4 = CalculateAUC(layoutDistanceChange.Select(x => (double)x).ToList());
                    incVariance4 = CalculateVariance(incLayoutDistanceChange.Select(x => (double)x).ToList());
                    variance4 = CalculateVariance(layoutDistanceChange.Select(x => (double)x).ToList());


                    incNearestNeighborWithin.Add(CalculateNearestNeighborWithin(incLayouts[i], incLayouts[i - 1]));
                    nearestNeighborWithin.Add(CalculateNearestNeighborWithin(layouts[i], layouts[i - 1]));
                    MAE5 = CalculateMAE(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    RMSE5 = CalculateRMSE(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    PearsonCorrelation5 = CalculatePearsonCorrelation(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    incAUC5 = CalculateAUC(incNearestNeighborWithin.Select(x => (double)x).ToList());
                    aUC5 = CalculateAUC(nearestNeighborWithin.Select(x => (double)x).ToList());
                    incVariance5 = CalculateVariance(incNearestNeighborWithin.Select(x => (double)x).ToList());
                    variance5 = CalculateVariance(nearestNeighborWithin.Select(x => (double)x).ToList());

                    incRanking.Add(CalculateRanking(incLayouts[i], incLayouts[i - 1]));
                    ranking.Add(CalculateRanking(layouts[i], layouts[i - 1]));
                    MAE6 = CalculateMAE(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    RMSE6 = CalculateRMSE(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    PearsonCorrelation6 = CalculatePearsonCorrelation(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    incAUC6 = CalculateAUC(incRanking.Select(x => (double)x).ToList());
                    aUC6 = CalculateAUC(ranking.Select(x => (double)x).ToList());
                    incVariance6 = CalculateVariance(incRanking.Select(x => (double)x).ToList());
                    variance6 = CalculateVariance(ranking.Select(x => (double)x).ToList());

                    incSERC.Add(CalculateSERC(incLayouts[i]));
                    sERC.Add(CalculateSERC(layouts[i]));
                    MAE7 = CalculateMAE(incSERC.Select(x => (double)x).ToList(), sERC.Select(x => (double)x).ToList());
                    RMSE7 = CalculateRMSE(incSERC.Select(x => (double)x).ToList(), sERC.Select(x => (double)x).ToList());
                    PearsonCorrelation7 = CalculatePearsonCorrelation(incSERC.Select(x => (double)x).ToList(), sERC.Select(x => (double)x).ToList());
                    incAUC7 = CalculateAUC(incSERC.Select(x => (double)x).ToList());
                    aUC7 = CalculateAUC(sERC.Select(x => (double)x).ToList());
                    incVariance7 = CalculateVariance(incSERC.Select(x => (double)x).ToList());
                    variance7 = CalculateVariance(sERC.Select(x => (double)x).ToList());

                    //incRelativeWeightChange.Add(CalculateRelativeWeightChange(incLayouts[i], incLayouts[i - 1]));
                    //relativeWeightChange.Add(CalculateRelativeWeightChange(layouts[i], layouts[i - 1]));

                }
            }


            //Debug.Log("incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists));
            //Debug.Log("Euclidean Distances: " + string.Join(", ", euclidianDists));
            string euclideanString = "";
            euclideanString += "Incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists) + "\n" + "#####################\n";
            euclideanString += "Euclidean Distances: " + string.Join(", ", euclidianDists) + "\n" + "\n";
            euclideanString += "MAE: " + MAE1 + ", RMSE: " + RMSE1 + ", Pearson Correlation: " + PearsonCorrelation1 + ", AUC: " + aUC1 + ", incAUC: " + incAUC1 + ", Variance: " + variance1 + ", incVariance: " + incVariance1;


            //Debug.Log("incremental ADN Distances: " + string.Join(", ", incADNDists));
            //Debug.Log("ADN Distances: " + string.Join(", ", ADNDists));
            string adnString = "";
            adnString += "Incremental ADN Distances: " + string.Join(", ", incADNDists) + "\n" + "#####################\n";
            adnString += "ADN Distances: " + string.Join(", ", ADNDists) + "\n" + "\n";
            adnString += "MAE: " + MAE2 + ", RMSE: " + RMSE2 + ", Pearson Correlation: " + PearsonCorrelation2 + ", AUC: " + aUC2 + ", incAUC: " + incAUC2 + ", Variance: " + variance2 + ", incVariance: " + incVariance2;

            //Debug.Log("incremental Average Relative Distances: " + string.Join(", ", incAverageRelativeDistance));
            //Debug.Log("Average Relative Distances: " + string.Join(", ", averageRelativeDistance));
            string averageRelativeDistanceString = "";
            averageRelativeDistanceString += "Incremental Average Relative Distances: " + string.Join(", ", incAverageRelativeDistance) + "\n" + "#####################\n";
            averageRelativeDistanceString += "Average Relative Distances: " + string.Join(", ", averageRelativeDistance) + "\n" + "\n";
            averageRelativeDistanceString += "MAE: " + MAE3 + ", RMSE: " + RMSE3 + ", Pearson Correlation: " + PearsonCorrelation3 + ", AUC: " + aUC3 + ", incAUC: " + incAUC3 + ", Variance: " + variance3 + ", incVariance: " + incVariance3;


            //Debug.Log("incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange));
            //Debug.Log("Layout Distance Changes: " + string.Join(", ", layoutDistanceChange));
            string layoutDistanceChangeString = "";
            layoutDistanceChangeString += "Incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange) + "\n" + "#####################\n";
            layoutDistanceChangeString += "Layout Distance Changes: " + string.Join(", ", layoutDistanceChange) + "\n" + "\n";
            layoutDistanceChangeString += "MAE: " + MAE4 + ", RMSE: " + RMSE4 + ", Pearson Correlation: " + PearsonCorrelation4 + ", AUC: " + aUC4 + ", incAUC: " + incAUC4 + ", Variance: " + variance4 + ", incVariance: " + incVariance4;


            //Debug.Log("incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin));
            //Debug.Log("Nearest Neighbor Within: " + string.Join(", ", nearestNeighborWithin));
            string nearestNeighborWithinString = "";
            nearestNeighborWithinString += "Incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin) + "\n" + "#####################\n";
            nearestNeighborWithinString += "Nearest Neighbor Within: " + string.Join(", ", nearestNeighborWithin) + "\n" + "\n";
            nearestNeighborWithinString += "MAE: " + MAE5 + ", RMSE: " + RMSE5 + ", Pearson Correlation: " + PearsonCorrelation5 + ", AUC: " + aUC5 + ", incAUC: " + incAUC5 + ", Variance: " + variance5 + ", incVariance: " + incVariance5;



            //Debug.Log("incremental Ranking: " + string.Join(", ", incRanking));
            //Debug.Log("Ranking: " + string.Join(", ", ranking));
            string rankingString = "";
            rankingString += "Incremental Ranking: " + string.Join(", ", incRanking) + "\n" + "#####################\n";
            rankingString += "Ranking: " + string.Join(", ", ranking) + "\n" + "\n";
            rankingString += "MAE: " + MAE6 + ", RMSE: " + RMSE6 + ", Pearson Correlation: " + PearsonCorrelation6 + ", AUC: " + aUC6 + ", incAUC: " + incAUC6 + ", Variance: " + variance6 + ", incVariance: " + incVariance6;


            //Debug.Log("incremental SERC: " + string.Join(", ", incSERC));
            //Debug.Log("SERC: " + string.Join(", ", sERC));
            string sercString = "";
            sercString += "Incremental SERC: " + string.Join(", ", incSERC) + "\n" + "#####################\n";
            sercString += "SERC: " + string.Join(", ", sERC) + "\n" + "\n";
            sercString += "MAE: " + MAE7 + ", RMSE: " + RMSE7 + ", Pearson Correlation: " + PearsonCorrelation7 + ", AUC: " + aUC7 + ", incAUC: " + incAUC7 + ", Variance: " + variance7 + ", incVariance: " + incVariance7;

            Debug.Log(euclideanString + "\n------------------------------- \n" +
              adnString + "\n------------------------------- \n" +
              averageRelativeDistanceString + "\n------------------------------- \n" +
              layoutDistanceChangeString + "\n------------------------------- \n" +
              nearestNeighborWithinString + "\n------------------------------- \n" +
              rankingString + "\n------------------------------- \n" +
              sercString);

            //Debug.Log("incremental Relative Weight Change: " + string.Join(", ", incRelativeWeightChange));
            //Debug.Log("Relative Weight Change: " + string.Join(", ", relativeWeightChange));

            Debug.Log("Total Time for Incremental Layout Evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSecondsIncremental));
            Debug.Log("Total Time for Layout Evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSeconds));

        }

        [Test]
        public void JustTestLayoutICPANDCP()
        {
            double totalTimeInMilliSecondsIncrementalCP = 0;
            double totalTimeInMilliSecondsCP = 0;

            List<List<LayoutVertex>> newlayoutVertexGroups = new List<List<LayoutVertex>>();

            List<Dictionary<ILayoutNode, NodeTransform>> incLayouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<IncrementalCirclePackingNodeLayout> incPackers = new List<IncrementalCirclePackingNodeLayout>();
            List<float> incEuclidianDists = new List<float>();
            List<float> incADNDists = new List<float>();
            List<float> incAverageRelativeDistance = new List<float>();
            List<float> incLayoutDistanceChange = new List<float>();
            List<float> incNearestNeighborWithin = new List<float>();
            List<float> incRanking = new List<float>();
            List<float> incSECC = new List<float>();
            List<float> incRelativeWeightChange = new List<float>();

            List<Dictionary<ILayoutNode, NodeTransform>> layouts = new List<Dictionary<ILayoutNode, NodeTransform>>();
            List<CirclePackingNodeLayout> packers = new List<CirclePackingNodeLayout>();
            List<float> euclidianDists = new List<float>();
            List<float> ADNDists = new List<float>();
            List<float> averageRelativeDistance = new List<float>();
            List<float> layoutDistanceChange = new List<float>();
            List<float> nearestNeighborWithin = new List<float>();
            List<float> ranking = new List<float>();
            List<float> sECC = new List<float>();
            List<float> relativeWeightChange = new List<float>();

            double MAE1 = 0;
            double RMSE1 = 0;
            double PearsonCorrelation1 = 0;
            double incAUC1 = 0;
            double aUC1 = 0;
            double incVariance1 = 0;
            double variance1 = 0;

            double MAE2 = 0;
            double RMSE2 = 0;
            double PearsonCorrelation2 = 0;
            double incAUC2 = 0;
            double aUC2 = 0;
            double incVariance2 = 0;
            double variance2 = 0;

            double MAE3 = 0;
            double RMSE3 = 0;
            double PearsonCorrelation3 = 0;
            double incAUC3 = 0;
            double aUC3 = 0;
            double incVariance3 = 0;
            double variance3 = 0;

            double MAE4 = 0;
            double RMSE4 = 0;
            double PearsonCorrelation4 = 0;
            double incAUC4 = 0;
            double aUC4 = 0;
            double incVariance4 = 0;
            double variance4 = 0;

            double MAE5 = 0;
            double RMSE5 = 0;
            double PearsonCorrelation5 = 0;
            double incAUC5 = 0;
            double aUC5 = 0;
            double incVariance5 = 0;
            double variance5 = 0;

            double MAE6 = 0;
            double RMSE6 = 0;
            double PearsonCorrelation6 = 0;
            double incAUC6 = 0;
            double aUC6 = 0;
            double incVariance6 = 0;
            double variance6 = 0;

            double MAE7 = 0;
            double RMSE7 = 0;
            double PearsonCorrelation7 = 0;
            double incAUC7 = 0;
            double aUC7 = 0;
            double incVariance7 = 0;
            double variance7 = 0;

            double MAE8 = 0;
            double RMSE8 = 0;
            double PearsonCorrelation8 = 0;
            double incAUC8 = 0;
            double aUC8 = 0;
            double incVariance8 = 0;
            double variance8 = 0;



            //newlayoutVertexGroups = JustMakeRandomGraphWithoutSave();
            newlayoutVertexGroups = LoadGraph();

            for (int i = 0; i < 100; i++)
            {
                incPackers.Add(new IncrementalCirclePackingNodeLayout());
                packers.Add(new CirclePackingNodeLayout());

                if (i > 0)
                {
                    incPackers[i].oldLayout = incPackers[i - 1];
                }
            }

            for (int i = 0; i < 100; i++)
            {
                Performance p = Performance.Begin("incremental CP Layout Evaluation");
                incLayouts.Add(incPackers[i].Create(newlayoutVertexGroups[i], Vector3.zero, Vector2.one));
                p.End();
                totalTimeInMilliSecondsIncrementalCP += p.GetTimeInMilliSeconds();

                Performance p2 = Performance.Begin("CP Layout Evaluation");
                layouts.Add(packers[i].Create(newlayoutVertexGroups[i], Vector3.zero, Vector2.one));
                p2.End();
                totalTimeInMilliSecondsCP += p2.GetTimeInMilliSeconds();

                if (i > 0)
                {
                    incEuclidianDists.Add(CalculateEuclideanMentalDistance(incLayouts[i], incLayouts[i - 1]));
                    euclidianDists.Add(CalculateEuclideanMentalDistance(layouts[i], layouts[i - 1]));
                    MAE1 = CalculateMAE(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    RMSE1 = CalculateRMSE(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    PearsonCorrelation1 = CalculatePearsonCorrelation(incEuclidianDists.Select(x => (double)x).ToList(), euclidianDists.Select(x => (double)x).ToList());
                    incAUC1 = CalculateAUC(incEuclidianDists.Select(x => (double)x).ToList());
                    aUC1 = CalculateAUC(euclidianDists.Select(x => (double)x).ToList());
                    incVariance1 = CalculateVariance(incEuclidianDists.Select(x => (double)x).ToList());
                    variance1 = CalculateVariance(euclidianDists.Select(x => (double)x).ToList());

                    incADNDists.Add(CalculateADN(incLayouts[i], incLayouts[i - 1]));
                    ADNDists.Add(CalculateADN(layouts[i], layouts[i - 1]));
                    MAE2 = CalculateMAE(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    RMSE2 = CalculateRMSE(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    PearsonCorrelation2 = CalculatePearsonCorrelation(incADNDists.Select(x => (double)x).ToList(), ADNDists.Select(x => (double)x).ToList());
                    incAUC2 = CalculateAUC(incADNDists.Select(x => (double)x).ToList());
                    aUC2 = CalculateAUC(ADNDists.Select(x => (double)x).ToList());
                    incVariance2 = CalculateVariance(incADNDists.Select(x => (double)x).ToList());
                    variance2 = CalculateVariance(ADNDists.Select(x => (double)x).ToList());

                    incAverageRelativeDistance.Add(CalculateAverageRelativeDistance(incLayouts[i], incLayouts[i - 1]));
                    averageRelativeDistance.Add(CalculateAverageRelativeDistance(layouts[i], layouts[i - 1]));
                    MAE3 = CalculateMAE(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    RMSE3 = CalculateRMSE(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    PearsonCorrelation3 = CalculatePearsonCorrelation(incAverageRelativeDistance.Select(x => (double)x).ToList(), averageRelativeDistance.Select(x => (double)x).ToList());
                    incAUC3 = CalculateAUC(incAverageRelativeDistance.Select(x => (double)x).ToList());
                    aUC3 = CalculateAUC(averageRelativeDistance.Select(x => (double)x).ToList());
                    incVariance3 = CalculateVariance(incAverageRelativeDistance.Select(x => (double)x).ToList());
                    variance3 = CalculateVariance(averageRelativeDistance.Select(x => (double)x).ToList());

                    incLayoutDistanceChange.Add(CalculateLayoutDistanceChange(incLayouts[i], incLayouts[i - 1]));
                    layoutDistanceChange.Add(CalculateLayoutDistanceChange(layouts[i], layouts[i - 1]));
                    MAE4 = CalculateMAE(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    RMSE4 = CalculateRMSE(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    PearsonCorrelation4 = CalculatePearsonCorrelation(incLayoutDistanceChange.Select(x => (double)x).ToList(), layoutDistanceChange.Select(x => (double)x).ToList());
                    incAUC4 = CalculateAUC(incLayoutDistanceChange.Select(x => (double)x).ToList());
                    aUC4 = CalculateAUC(layoutDistanceChange.Select(x => (double)x).ToList());
                    incVariance4 = CalculateVariance(incLayoutDistanceChange.Select(x => (double)x).ToList());
                    variance4 = CalculateVariance(layoutDistanceChange.Select(x => (double)x).ToList());


                    incNearestNeighborWithin.Add(CalculateNearestNeighborWithin(incLayouts[i], incLayouts[i - 1]));
                    nearestNeighborWithin.Add(CalculateNearestNeighborWithin(layouts[i], layouts[i - 1]));
                    MAE5 = CalculateMAE(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    RMSE5 = CalculateRMSE(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    PearsonCorrelation5 = CalculatePearsonCorrelation(incNearestNeighborWithin.Select(x => (double)x).ToList(), nearestNeighborWithin.Select(x => (double)x).ToList());
                    incAUC5 = CalculateAUC(incNearestNeighborWithin.Select(x => (double)x).ToList());
                    aUC5 = CalculateAUC(nearestNeighborWithin.Select(x => (double)x).ToList());
                    incVariance5 = CalculateVariance(incNearestNeighborWithin.Select(x => (double)x).ToList());
                    variance5 = CalculateVariance(nearestNeighborWithin.Select(x => (double)x).ToList());

                    incRanking.Add(CalculateRanking(incLayouts[i], incLayouts[i - 1]));
                    ranking.Add(CalculateRanking(layouts[i], layouts[i - 1]));
                    MAE6 = CalculateMAE(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    RMSE6 = CalculateRMSE(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    PearsonCorrelation6 = CalculatePearsonCorrelation(incRanking.Select(x => (double)x).ToList(), ranking.Select(x => (double)x).ToList());
                    incAUC6 = CalculateAUC(incRanking.Select(x => (double)x).ToList());
                    aUC6 = CalculateAUC(ranking.Select(x => (double)x).ToList());
                    incVariance6 = CalculateVariance(incRanking.Select(x => (double)x).ToList());
                    variance6 = CalculateVariance(ranking.Select(x => (double)x).ToList());

                    incSECC.Add(CalculateSECC(incLayouts[i], Vector3.zero));
                    sECC.Add(CalculateSECC(layouts[i], Vector3.zero));
                    MAE7 = CalculateMAE(incSECC.Select(x => (double)x).ToList(), sECC.Select(x => (double)x).ToList());
                    RMSE7 = CalculateRMSE(incSECC.Select(x => (double)x).ToList(), sECC.Select(x => (double)x).ToList());
                    PearsonCorrelation7 = CalculatePearsonCorrelation(incSECC.Select(x => (double)x).ToList(), sECC.Select(x => (double)x).ToList());
                    incAUC7 = CalculateAUC(incSECC.Select(x => (double)x).ToList());
                    aUC7 = CalculateAUC(sECC.Select(x => (double)x).ToList());
                    incVariance7 = CalculateVariance(incSECC.Select(x => (double)x).ToList());
                    variance7 = CalculateVariance(sECC.Select(x => (double)x).ToList());


                    //incRelativeWeightChange.Add(CalculateRelativeWeightChange(incLayouts[i], incLayouts[i - 1]));
                    //relativeWeightChange.Add(CalculateRelativeWeightChange(layouts[i], layouts[i - 1]));

                }
            }

            //Debug.Log("incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists));
            //Debug.Log("Euclidean Distances: " + string.Join(", ", euclidianDists));
            string euclideanString = "";
            euclideanString += "CP Incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists) + "\n" + "#####################\n";
            euclideanString += "CP Euclidean Distances: " + string.Join(", ", euclidianDists) + "\n" + "\n";
            euclideanString += "MAE: " + MAE1 + ", RMSE: " + RMSE1 + ", Pearson Correlation: " + PearsonCorrelation1 + ", AUC: " + aUC1 + ", incAUC: " + incAUC1 + ", Variance: " + variance1 + ", incVariance: " + incVariance1;


            //Debug.Log("incremental ADN Distances: " + string.Join(", ", incADNDists));
            //Debug.Log("ADN Distances: " + string.Join(", ", ADNDists));
            string adnString = "";
            adnString += "CP Incremental ADN Distances: " + string.Join(", ", incADNDists) + "\n" + "#####################\n";
            adnString += "CP ADN Distances: " + string.Join(", ", ADNDists) + "\n" + "\n";
            adnString += "MAE: " + MAE2 + ", RMSE: " + RMSE2 + ", Pearson Correlation: " + PearsonCorrelation2 + ", AUC: " + aUC2 + ", incAUC: " + incAUC2 + ", Variance: " + variance2 + ", incVariance: " + incVariance2;

            //Debug.Log("incremental Average Relative Distances: " + string.Join(", ", incAverageRelativeDistance));
            //Debug.Log("Average Relative Distances: " + string.Join(", ", averageRelativeDistance));
            string averageRelativeDistanceString = "";
            averageRelativeDistanceString += "CP Incremental Average Relative Distances: " + string.Join(", ", incAverageRelativeDistance) + "\n" + "#####################\n";
            averageRelativeDistanceString += "CP Average Relative Distances: " + string.Join(", ", averageRelativeDistance) + "\n" + "\n";
            averageRelativeDistanceString += "MAE: " + MAE3 + ", RMSE: " + RMSE3 + ", Pearson Correlation: " + PearsonCorrelation3 + ", AUC: " + aUC3 + ", incAUC: " + incAUC3 + ", Variance: " + variance3 + ", incVariance: " + incVariance3;


            //Debug.Log("incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange));
            //Debug.Log("Layout Distance Changes: " + string.Join(", ", layoutDistanceChange));
            string layoutDistanceChangeString = "";
            layoutDistanceChangeString += "CP Incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange) + "\n" + "#####################\n";
            layoutDistanceChangeString += "CP Layout Distance Changes: " + string.Join(", ", layoutDistanceChange) + "\n" + "\n";
            layoutDistanceChangeString += "MAE: " + MAE4 + ", RMSE: " + RMSE4 + ", Pearson Correlation: " + PearsonCorrelation4 + ", AUC: " + aUC4 + ", incAUC: " + incAUC4 + ", Variance: " + variance4 + ", incVariance: " + incVariance4;


            //Debug.Log("incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin));
            //Debug.Log("Nearest Neighbor Within: " + string.Join(", ", nearestNeighborWithin));
            string nearestNeighborWithinString = "";
            nearestNeighborWithinString += "CP Incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin) + "\n" + "#####################\n";
            nearestNeighborWithinString += "CP Nearest Neighbor Within: " + string.Join(", ", nearestNeighborWithin) + "\n" + "\n";
            nearestNeighborWithinString += "MAE: " + MAE5 + ", RMSE: " + RMSE5 + ", Pearson Correlation: " + PearsonCorrelation5 + ", AUC: " + aUC5 + ", incAUC: " + incAUC5 + ", Variance: " + variance5 + ", incVariance: " + incVariance5;



            //Debug.Log("incremental Ranking: " + string.Join(", ", incRanking));
            //Debug.Log("Ranking: " + string.Join(", ", ranking));
            string rankingString = "";
            rankingString += "CP Incremental Ranking: " + string.Join(", ", incRanking) + "\n" + "#####################\n";
            rankingString += "CP Ranking: " + string.Join(", ", ranking) + "\n" + "\n";
            rankingString += "MAE: " + MAE6 + ", RMSE: " + RMSE6 + ", Pearson Correlation: " + PearsonCorrelation6 + ", AUC: " + aUC6 + ", incAUC: " + incAUC6 + ", Variance: " + variance6 + ", incVariance: " + incVariance6;


            //Debug.Log("incremental SERC: " + string.Join(", ", incSERC));
            //Debug.Log("SERC: " + string.Join(", ", sERC));
            string sercString = "";
            sercString += "CP Incremental SECC: " + string.Join(", ", incSECC) + "\n" + "#####################\n";
            sercString += "CP SECC: " + string.Join(", ", sECC) + "\n" + "\n";
            sercString += "MAE: " + MAE7 + ", RMSE: " + RMSE7 + ", Pearson Correlation: " + PearsonCorrelation7 + ", AUC: " + aUC7 + ", incAUC: " + incAUC7 + ", Variance: " + variance7 + ", incVariance: " + incVariance7;

            Debug.Log(euclideanString + "\n------------------------------- \n" +
              adnString + "\n------------------------------- \n" +
              averageRelativeDistanceString + "\n------------------------------- \n" +
              layoutDistanceChangeString + "\n------------------------------- \n" +
              nearestNeighborWithinString + "\n------------------------------- \n" +
              rankingString + "\n------------------------------- \n" +
              sercString);

            //Debug.Log("incremental Euclidean Distances: " + string.Join(", ", incEuclidianDists));
            //Debug.Log("Euclidean Distances: " + string.Join(", ", euclidianDists));

            //Debug.Log("incremental ADN Distances: " + string.Join(", ", incADNDists));
            //Debug.Log("ADN Distances: " + string.Join(", ", ADNDists));

            //Debug.Log("incremental Average Relative Distances: " + string.Join(", ", incAverageRelativeDistance));
            //Debug.Log("Average Relative Distances: " + string.Join(", ", averageRelativeDistance));

            //Debug.Log("incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange));
            //Debug.Log("Layout Distance Changes: " + string.Join(", ", layoutDistanceChange));


            //Debug.Log("incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin));
            //Debug.Log("Nearest Neighbor Within: " + string.Join(", ", nearestNeighborWithin));


            //Debug.Log("incremental Ranking: " + string.Join(", ", incRanking));
            //Debug.Log("Ranking: " + string.Join(", ", ranking));
            //Debug.Log("MAE: " + MAE + ", RMSE: " + RMSE + ", Pearson Correlation: " + PearsonCorrelation + ", AUC: " + aUC + ", incAUC: " + incAUC + ", Variance: " + variance + ", incVariance: " + incVariance);
            //string rankingString = "";
            //rankingString += "Incremental Ranking: " + string.Join(", ", incRanking) + "\n" + "#####################\n";
            //rankingString += "Ranking: " + string.Join(", ", ranking) + "\n" + "\n";
            //rankingString += "MAE: " + MAE + ", RMSE: " + RMSE + ", Pearson Correlation: " + PearsonCorrelation + ", AUC: " + aUC + ", incAUC: " + incAUC + ", Variance: " + variance + ", incVariance: " + incVariance;
            //Debug.Log(rankingString);


            //Debug.Log("incremental SECC: " + string.Join(", ", incSECC));
            //Debug.Log("SECC: " + string.Join(", ", sECC));
            //string SECCString = "";
            //SECCString += "Incremental SECC: " + string.Join(", ", incSECC) + "\n" + "#####################\n";
            //SECCString += "SECC: " + string.Join(", ", sECC) + "\n" + "\n";
            //SECCString += "MAE: " + MAE + ", RMSE: " + RMSE + ", Pearson Correlation: " + PearsonCorrelation + ", AUC: " + aUC + ", incAUC: " + incAUC + ", Variance: " + variance + ", incVariance: " + incVariance;
            //Debug.Log(SECCString);


            //Debug.Log("incremental Relative Weight Change: " + string.Join(", ", incRelativeWeightChange));
            //Debug.Log("Relative Weight Change: " + string.Join(", ", relativeWeightChange));

            Debug.Log("Total Time for Incremental CP Layout Evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSecondsIncrementalCP));
            Debug.Log("Total Time for CP Layout Evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSecondsCP));

        }

        #endregion
    }
}

