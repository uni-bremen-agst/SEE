using MoreLinq;
using NUnit.Framework;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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
    [Test]
    public void TestMerge()
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
      Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);

      // Merge El1

      tree.MergeFreeLeaves(result);
      Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { A }), Is.True);

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

    [Test]
    public void TestObvious()
    {
      Assert.True(true);
      var a = new Vector2(5.0f, 5.0f) - new Vector2(10.0f, 10.0f);
      Debug.Log(a);
    }

    /// <summary>
    /// Let's us explore performance issues.
    /// </summary>
    [Test]
    public void TestLayout()
    {
      /*
      //Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
      Dictionary<ILayoutNode, NodeTransform> secondLayout = new Dictionary<ILayoutNode, NodeTransform>();
       */
      LayoutVertex node1 = new(new Vector3(8, 1, 6), 1);
      LayoutVertex node2 = new(new Vector3(7, 1, 3), 2);
      LayoutVertex node3 = new(new Vector3(5, 1, 3), 3);
      LayoutVertex node4 = new(new Vector3(4, 1, 4), 4);
      IEnumerable<ILayoutNode> nodes1 = new[] { node1};
      IEnumerable<ILayoutNode> nodes2 = new[] { node1 , node2};
      IEnumerable<ILayoutNode> nodes3 = new[] { node1, node2, node3 };
      IEnumerable<ILayoutNode> nodes4 = new[] { node1, node2, node3, node4 };

      RectanglePackingNodeLayout2 packer1 = new();
      RectanglePackingNodeLayout2 packer2 = new();
      RectanglePackingNodeLayout2 packer3 = new();
      RectanglePackingNodeLayout2 packer4 = new();

      Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);

      RectanglePackingNodeLayout2.tree.Print();

      packer2.oldLayout = packer1;

      Dictionary<ILayoutNode, NodeTransform> secondLayout = packer2.Create(nodes2, Vector3.zero, Vector2.one);

      RectanglePackingNodeLayout2.tree.Print();

      packer3.oldLayout = packer2;

      Dictionary<ILayoutNode, NodeTransform> thirdLayout = packer3.Create(nodes3, Vector3.zero, Vector2.one);

      RectanglePackingNodeLayout2.tree.Print();

      foreach (var entry in packer3.layoutResult.ToList())
      {
        if (entry.Key.ID == "1")
        {
          Debug.Log("here");
          ILayoutNode vertex = new LayoutVertex(new Vector3(3, 1, 3), 1);
          // Remove the old key and add the new key-value pair
          packer3.layoutResult.Remove(entry.Key);
          packer3.layoutResult[vertex] = entry.Value;
        }
      }
      /*
       */
      packer4.oldLayout = packer3;

      Dictionary<ILayoutNode, NodeTransform> forthLayout = packer4.Create(nodes4, Vector3.zero, Vector2.one);
      /*


      //*************************************************************************************************************
      Assert.NotNull(packer1);
      Assert.IsTrue(nodes1.Count()>0);
      foreach (ILayoutNode node in nodes1)
      {
        if (node.IsLeaf)
        {
          var scale = node.AbsoluteScale;
          firstLayout[node] = new NodeTransform(Vector3.zero, scale);
        }
      }

      Debug.Log("1");

      Vector2 area1 = Vector2.zero;

      if (packer1.AllAreLeaves(nodes1))
      {
        area1 = packer1.Pack(firstLayout, nodes1.ToList(), 0f);
      }
      else
      {
        var root = LayoutNodes.GetRoots(nodes1).FirstOrDefault();
        area1 = packer1.PlaceNodes(firstLayout, root, 0);
        firstLayout[root] = new NodeTransform(0, 0, new Vector3(area1.x, root.AbsoluteScale.y, area1.y));
      }

      //*************************************************************************************************************
      firstlayout is set 
      in the second layout we set the old layout to the first layout
      the same nodes in second layout are 

      packer2.oldLayout = packer1;

      Assert.NotNull(packer2);
      Assert.IsTrue(nodes2.Count() > 0);

      

      var oldLeavesIDs = packer2.oldLayout.layoutResult.Keys.Where(node => node.IsLeaf).Select(node => node.ID).ToList();
      Assert.NotNull(oldLeavesIDs);
      var leaveInBothLayouts = nodes2.Where(n => n.IsLeaf && oldLeavesIDs.Contains(n.ID)).ToList();
      Debug.LogFormat("Nodes in both layouts: {0}\n", leaveInBothLayouts.Count);

      foreach (ILayoutNode node in leaveInBothLayouts)
      {
        if (node.IsLeaf)
        {
          var scale = node.AbsoluteScale;
          secondLayout[node] = new NodeTransform(Vector3.zero, scale);
        }
      }

      Debug.Log("2");

      Vector2 area2 = Vector2.zero;

      if (packer2.AllAreLeaves(leaveInBothLayouts))
      {
        area2 = packer2.Pack(secondLayout, leaveInBothLayouts.ToList(), 0f);
      }
      else
      {
        var root = LayoutNodes.GetRoots(leaveInBothLayouts).FirstOrDefault();
        area2 = packer2.PlaceNodes(secondLayout, root, 0);
        secondLayout[root] = new NodeTransform(0, 0, new Vector3(area2.x, root.AbsoluteScale.y, area2.y));
      }
       */

      //*************************************************************************************************************
      RectanglePackingNodeLayout2.tree.Print();
      RectanglePackingNodeLayout2.tree = null;

    }

    [Test]
    public void TestLayout1()
    {
      ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes();

      RectanglePackingNodeLayout packer = new();

      Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
    }

    /*
     Test packer 
    [Test]
    public void TestPacker()
    {
      Vector2 totalSize = new(20, 20);
      PTree tree = new(Vector2.zero, totalSize);
      RectanglePackingNodeLayout packer = new();
      ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes();

      List<Vector2> rectanglesToPack = new()
      {
        new Vector2(5, 5),
        new Vector2(7, 3),
        new Vector2(4, 6),
        new Vector2(6, 4),
        new Vector2(3, 8),
        new Vector2(8, 2),
      };
      List<PNode> packedRectangles = packer.PackRectangles(rectanglesToPack);
      Assert.That(packedRectangles.Count, Is.EqualTo(rectanglesToPack.Count));
      foreach (PNode node in packedRectangles)
      {
        Debug.LogFormat("Packed rectangle at position {0} with size {1}\n",
            node.Rectangle.Position, node.Rectangle.Size);
      }
    }
     */

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

      var oldRootSize = tree.Root.Rectangle.Size;

      tree.Root.Rectangle.Size = new Vector2(20, 20);
      // Adjust free leaves
      tree.FreeLeavesAdjust(oldRootSize);
      tree.Print();
      Assert.That(EqualLists(tree.FreeLeaves, new List<PNode>() { C, D }), Is.True);
    }
  }
}

