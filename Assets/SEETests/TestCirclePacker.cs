using MoreLinq;
using NUnit.Framework;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Sprites;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace SEE.Layout.CirclePacking
{
  
  internal class TestCirclePacker
  {

    /*
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
     Test PTree merge and split operations.
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
     */


    [Test]
    public void TestLayout()
    {
      
      RectanglePackingNodeLayout2.tree.Print();

    }

    //*************************************************************************************************************
    [Test]
    public void TestLayout1()
    {
      ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

      CirclePackingNodeLayout packer = new();

      Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
    }

    //*************************************************************************************************************
    [Test]
    public void TestLayout2()
    {
      ICollection<ILayoutNode> gameObjects = NodeCreator.CreateNodes(1);

      CirclePackingNodeLayout packer = new();

      Dictionary<ILayoutNode, NodeTransform> layout = packer.Create(gameObjects, Vector3.zero, Vector2.one);
    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutZSRL3GrowLeafWithPacker1()
    {
      
      LayoutVertex node1 = new(new Vector3(0.8f, 1, 0.6f), 1);

      IEnumerable<ILayoutNode> nodes1 = new[] { node1 };


      IncrementalCirclePackingNodeLayout packer1 = new();


      Dictionary<ILayoutNode, NodeTransform> firstLayout = packer1.Create(nodes1, Vector3.zero, Vector2.one);



      


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker1()
    {
      
      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex parent = new("parent");

      parent.AddChild(node1);
      parent.AddChild(node2);

      IncrementalCirclePackingNodeLayout packer1 = new();
      Dictionary<ILayoutNode, NodeTransform> layout = new();

      packer1.PlaceNodes(parent, layout);

      //Circle1 circle1 = new Circle1(node1, node1.AbsoluteScale.x / 2, node1.AbsoluteScale.z / 2);

      //IncrementalCirclePacker.PackCircles(new List<Circle1>() { new Circle1(node1.AbsoluteScale.x / 2, node1.AbsoluteScale.z / 2) }, Vector2.zero, out float containerRadius);
      


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker2()
    {


      Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");
      

      Circle1 circle1 = new Circle1(new Vector2(10.0f, 0f), 2.0f, "2");


      Circle1 circle2 = new Circle1(new Vector2(0f, 10f), 2.0f, "3");
      

      Circle1 surroundingCircle = IncrementalCirclePacker.ComputeSurroundingCircle11(new List<Circle1>() { circle1, circle2 });

      Debug.Log(surroundingCircle.ToString());

      Circle1 surroundingCircleReset = IncrementalCirclePacker.ComputeSurroundingCircle11ResetCircles(new List<Circle1>() { circle1, circle2 });

      Debug.Log(surroundingCircleReset.ToString());
      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());




    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker3()
    {


      //Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");


      //Circle1 circle1 = new Circle1(Vector2.zero, 2.0f, "1");
      //Circle1 circle2 = new Circle1(Vector2.zero, 2.0f, "2");
      //Circle1 circle3 = new Circle1(Vector2.zero, 2.0f, "3");
      //Circle1 circle4 = new Circle1(Vector2.zero, 2.0f, "4");


      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1}, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
      

    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker4ExpandCircleA()
    {


      Circle1 root = new Circle1(Vector2.zero, 10.0f, "root");


      Circle1 circle1 = new Circle1(new Vector2(10.0f, 0f), 2.0f, "1");


      Circle1 circle2 = new Circle1(new Vector2(0f, 10f), 2.0f, "2");

      Circle1 circle3 = new Circle1(new Vector2(0f, 0f), 2.0f, "3");

      List<Circle1> circles = new List<Circle1>() { circle1, circle2, circle3 };


      IncrementalCirclePacker.ExpandFromCircleA(circles, circle2, 5.0f);


      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());
      Debug.Log(circle3.ToString());




    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker5SpatialHashGrid()
    {


      Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");


      //Circle1 circle1 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "1");
      //Circle1 circle2 = new Circle1(new Vector2(0.2f, 0.2f), 0.2f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.3f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.4f, "4");

      Circle1 circle1 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.3f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.4f, "4");


      SpatialHashGrid grid = new SpatialHashGrid(0.1f);
      grid.Insert(circle1);
      grid.Insert(circle2);
      //grid.Insert(circle3);
      //grid.Insert(circle4);

      /*
      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1 }, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
       
       */

      //Debug.Log(circle1.ToString());
      //Debug.Log(circle2.ToString());
      //Debug.Log(circle3.ToString());
      //Debug.Log(circle4.ToString());

      grid._cells.ToList().ForEach(cell =>
      {
        Debug.LogFormat("Cell {0} contains circles:\n", cell.Key);
        foreach (Circle1 circle in cell.Value)
        {
          Debug.LogFormat("Circle in cell: {0}\n", circle.ToString());
        }
      });

      //var nearbyCircles = grid.GetNearby(new Circle1(new Vector2(0.15f, 0.15f), 0.1f, "test"));
      var nearbyCircles = grid.GetNearby(circle1);

      Debug.Log("Nearby circles to test circle:\n");
      foreach (Circle1 circle in nearbyCircles)
      {
        Debug.LogFormat("Nearby circle: {0}\n", circle.ToString());
      }

      //IncrementalCirclePacker.PackCircles(new List<Circle1>() { circle1, circle2 }, Vector2.zero, out float containerRadius, false, "parent");


    }


    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker6CirclePacker2()
    {
      Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(0.2f, 0.2f), 0.2f, "2");
      Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.3f, "3");
      Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.4f, "4");


      /*
      Circle1 circle1 = new Circle1(Vector2.zero, 0.1f, "1");
      Circle1 circle2 = new Circle1(Vector2.zero, 0.2f, "2");
      Circle1 circle3 = new Circle1(Vector2.zero, 0.3f, "3");
      Circle1 circle4 = new Circle1(Vector2.zero, 0.4f, "4");


      List<Circle1> circles = new List<Circle1>() { circle1, circle2, circle3, circle4 };

      int i = 0;
      foreach (var child in circles)
      {
        float radians = (i / (float)circles.Count) * (2.0f * Mathf.PI);
        child.Center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 0.1f;
        i++;
      }
       */

      /*
      SpatialHashGrid grid = new SpatialHashGrid(0.1f);
      grid.Insert(circle1);
      grid.Insert(circle2);
      grid.Insert(circle3);
      grid.Insert(circle4);
       */

      CirclePacker2 packer2 = new CirclePacker2(0.1f);
      packer2.GravityStrength = 5.0f; // Move 1 unit per step
      packer2.PbdIterations = 100;


      packer2.AddCircle(circle1);
      packer2.AddCircle(circle2);
      packer2.AddCircle(circle3);
      packer2.AddCircle(circle4);

      packer2.ComputePacking(10, new List<Circle1>() { circle1, circle2, circle3, circle4 });

      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());
      Debug.Log(circle3.ToString());
      Debug.Log(circle4.ToString());


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker7CirclePacker2ResolveCollision()
    {
      //Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.1f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.1f, "4");
      List<Circle1> circles = new List<Circle1>() { circle1, circle2};


      /*
      Circle1 circle1 = new Circle1(Vector2.zero, 0.1f, "1");
      Circle1 circle2 = new Circle1(Vector2.zero, 0.2f, "2");
      Circle1 circle3 = new Circle1(Vector2.zero, 0.3f, "3");
      Circle1 circle4 = new Circle1(Vector2.zero, 0.4f, "4");



      int i = 0;
      foreach (var child in circles)
      {
        float radians = (i / (float)circles.Count) * (2.0f * Mathf.PI);
        child.Center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 0.1f;
        i++;
      }
       */

      /*
      SpatialHashGrid grid = new SpatialHashGrid(0.1f);
      grid.Insert(circle1);
      grid.Insert(circle2);
      grid.Insert(circle3);
      grid.Insert(circle4);
       */

      CirclePacker2 packer2 = new CirclePacker2(0.1f);
      //packer2.GravityStrength = 5.0f; // Move 1 unit per step
      //packer2.PbdIterations = 100;


      packer2.AddCircle(circle1);
      packer2.AddCircle(circle2);
      //packer2.AddCircle(circle3);
      //packer2.AddCircle(circle4);


      packer2.RebuildGrid(circles);
      Debug.Log(packer2.ResolveCollisions(circles));
      foreach (Circle1 circle in circles)
      {
        Debug.Log(circle.ToString());
      }

      /*
      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1 }, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
       
       */
      
      /*
       
      grid._cells.ToList().ForEach(cell =>
      {
        Debug.LogFormat("Cell {0} contains circles:\n", cell.Key);
        foreach (Circle1 circle in cell.Value)
        {
          Debug.LogFormat("Circle in cell: {0}\n", circle.ToString());
        }
      });

      var nearbyCircles = grid.GetNearby(new Circle1(new Vector2(0.15f, 0.15f), 0.1f, "test"));

      Debug.Log("Nearby circles to test circle:\n");
      foreach (Circle1 circle in nearbyCircles)
      {
        Debug.LogFormat("Nearby circle: {0}\n", circle.ToString());
      }
       */


      //IncrementalCirclePacker.PackCircles(new List<Circle1>() { circle1, circle2 }, Vector2.zero, out float containerRadius, false, "parent");


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker7CirclePacker2ApplyGravity()
    {
      //Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0.2f, 0.9f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(-0.5f, 0.7f), 0.1f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.1f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.1f, "4");
      List<Circle1> circles = new List<Circle1>() { circle1, circle2 };


      /*
      Circle1 circle1 = new Circle1(Vector2.zero, 0.1f, "1");
      Circle1 circle2 = new Circle1(Vector2.zero, 0.2f, "2");
      Circle1 circle3 = new Circle1(Vector2.zero, 0.3f, "3");
      Circle1 circle4 = new Circle1(Vector2.zero, 0.4f, "4");



      int i = 0;
      foreach (var child in circles)
      {
        float radians = (i / (float)circles.Count) * (2.0f * Mathf.PI);
        child.Center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 0.1f;
        i++;
      }
       */

      /*
      SpatialHashGrid grid = new SpatialHashGrid(0.1f);
      grid.Insert(circle1);
      grid.Insert(circle2);
      grid.Insert(circle3);
      grid.Insert(circle4);
       */

      CirclePacker2 packer2 = new CirclePacker2(0.1f);
      //packer2.GravityStrength = 5.0f; // Move 1 unit per step
      //packer2.PbdIterations = 100;


      //packer2.AddCircle(circle1);
      //packer2.AddCircle(circle2);
      //packer2.AddCircle(circle3);
      //packer2.AddCircle(circle4);

      // 1. Apply Gravity
      for (int i = 0; i < 100; i++)
      {
        foreach (var c in circles)
        {
          float dx = 0 - c.X;
          float dy = 0 - c.Y;
          float dist = MathF.Sqrt(dx * dx + dy * dy);

          if (dist > 0)
          {
            c.X += (dx / dist) * 0.1f;
            c.Y += (dy / dist) * 0.1f;
          }
        }

      }
      packer2.RebuildGrid(circles);
      Debug.Log(packer2.ResolveCollisions(circles));

      foreach (Circle1 circle in circles)
      {
        Debug.Log(circle.ToString());
      }
       

      /*
       */


      /*
      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1 }, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
       
       */

      /*
       
      grid._cells.ToList().ForEach(cell =>
      {
        Debug.LogFormat("Cell {0} contains circles:\n", cell.Key);
        foreach (Circle1 circle in cell.Value)
        {
          Debug.LogFormat("Circle in cell: {0}\n", circle.ToString());
        }
      });

      var nearbyCircles = grid.GetNearby(new Circle1(new Vector2(0.15f, 0.15f), 0.1f, "test"));

      Debug.Log("Nearby circles to test circle:\n");
      foreach (Circle1 circle in nearbyCircles)
      {
        Debug.LogFormat("Nearby circle: {0}\n", circle.ToString());
      }
       */


      //IncrementalCirclePacker.PackCircles(new List<Circle1>() { circle1, circle2 }, Vector2.zero, out float containerRadius, false, "parent");


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker7CirclePacker2ApplyGravityAndResolveConflicts()
    {
      //Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(0.1f, 0.1f), 0.1f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.1f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.1f, "4");
      List<Circle1> circles = new List<Circle1>() { circle1, circle2 };


      /*
      Circle1 circle1 = new Circle1(Vector2.zero, 0.1f, "1");
      Circle1 circle2 = new Circle1(Vector2.zero, 0.2f, "2");
      Circle1 circle3 = new Circle1(Vector2.zero, 0.3f, "3");
      Circle1 circle4 = new Circle1(Vector2.zero, 0.4f, "4");



      int i = 0;
      foreach (var child in circles)
      {
        float radians = (i / (float)circles.Count) * (2.0f * Mathf.PI);
        child.Center = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 0.1f;
        i++;
      }
       */

      /*
      SpatialHashGrid grid = new SpatialHashGrid(0.1f);
      grid.Insert(circle1);
      grid.Insert(circle2);
      grid.Insert(circle3);
      grid.Insert(circle4);
       */

      CirclePacker2 packer2 = new CirclePacker2(0.1f);
      //packer2.GravityStrength = 5.0f; // Move 1 unit per step
      //packer2.PbdIterations = 100;


      //packer2.AddCircle(circle1);
      //packer2.AddCircle(circle2);
      //packer2.AddCircle(circle3);
      //packer2.AddCircle(circle4);

      // 1. Apply Gravity
      for (int i = 0; i < 100; i++)
      {
        foreach (var c in circles)
        {
          float dx = 0 - c.X;
          float dy = 0 - c.Y;
          float dist = MathF.Sqrt(dx * dx + dy * dy);

          if (dist > 0)
          {
            c.X += (dx / dist) * 0.1f;
            c.Y += (dy / dist) * 0.1f;
          }
        }

      }
      packer2.RebuildGrid(circles);
      Debug.Log(packer2.ResolveCollisions(circles));

      foreach (Circle1 circle in circles)
      {
        Debug.Log(circle.ToString());
      }


      /*
       */


      /*
      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1 }, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
       
       */

      /*
       
      grid._cells.ToList().ForEach(cell =>
      {
        Debug.LogFormat("Cell {0} contains circles:\n", cell.Key);
        foreach (Circle1 circle in cell.Value)
        {
          Debug.LogFormat("Circle in cell: {0}\n", circle.ToString());
        }
      });

      var nearbyCircles = grid.GetNearby(new Circle1(new Vector2(0.15f, 0.15f), 0.1f, "test"));

      Debug.Log("Nearby circles to test circle:\n");
      foreach (Circle1 circle in nearbyCircles)
      {
        Debug.LogFormat("Nearby circle: {0}\n", circle.ToString());
      }
       */


      //IncrementalCirclePacker.PackCircles(new List<Circle1>() { circle1, circle2 }, Vector2.zero, out float containerRadius, false, "parent");


    }

    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker8ExpandFromCircleA()
    {
      //Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0f, 0f), 0.1f, "1");
      Circle1 circle2 = new Circle1(new Vector2(0.2f, 0f), 0.1f, "2");
      Circle1 circle3 = new Circle1(new Vector2(-0.2f, 0f), 0.1f, "3");
      Circle1 circle4 = new Circle1(new Vector2(0f, 0.2f), 0.1f, "4");
      Circle1 circle5 = new Circle1(new Vector2(0f, -0.2f), 0.1f, "5");

      List<Circle1> circles = new List<Circle1>() { circle1, circle2, circle3, circle4, circle5 };


      //circle1.Radius = 1f;

      IncrementalCirclePacker.ExpandFromCircleA(circles, circle2, 1f);

      foreach (Circle1 circle in circles)
      {
        Debug.Log(circle.ToString());
      }


    }


    //*************************************************************************************************************
    [Test]
    public void TestLayoutCirclePacker6CirclePacker2_2()
    {
      Circle1 root = new Circle1(Vector2.zero, 10.0f, "1");

      Circle1 circle1 = new Circle1(new Vector2(0f, 0f), 0.06428244f, "1");
      Circle1 circle2 = new Circle1(new Vector2(-0.62f, -0.45f), 0.06428244f, "2");
      //Circle1 circle3 = new Circle1(new Vector2(0.3f, 0.3f), 0.3f, "3");
      //Circle1 circle4 = new Circle1(new Vector2(0.4f, 0.4f), 0.4f, "4");
      Debug.Log("0");
      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());


      CirclePacker2 packer2 = new CirclePacker2(0.1f);
      packer2.PbdIterations = 10;


      packer2.ComputePacking(500, new List<Circle1>() { circle1, circle2});

      Debug.Log("1");
      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());


      circle1 = new Circle1(new Vector2(0f, 0f), 0.06428244f, "1");
      circle2 = new Circle1(new Vector2(-0.62f, -0.45f), 0.06428244f, "2");





      packer2.ComputePacking(500, new List<Circle1>() { circle1, circle2 });

      /*
      LayoutVertex root = new("root");

      LayoutVertex node1 = new(new Vector3(0.1f, 1, 0.1f), 1);
      LayoutVertex node2 = new(new Vector3(0.1f, 1, 0.1f), 2);
      LayoutVertex node3 = new(new Vector3(0.1f, 1, 0.1f), 3);
      LayoutVertex node4 = new(new Vector3(0.1f, 1, 0.1f), 4);



      IncrementalCirclePackingNodeLayout packer1 = new IncrementalCirclePackingNodeLayout();
      root.AddChild(node1);
      packer1.Create(new[] { root, node1 }, Vector3.zero, Vector2.one);


      IncrementalCirclePackingNodeLayout packer2 = new IncrementalCirclePackingNodeLayout();

      packer2.oldLayout = packer1;

      root.AddChild(node2);
      packer2.Create(new[] { root, node1, node2 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer3 = new IncrementalCirclePackingNodeLayout();

      packer3.oldLayout = packer2;

      root.AddChild(node3);
      packer3.Create(new[] { root, node1, node2, node3 }, Vector3.zero, Vector2.one);

      IncrementalCirclePackingNodeLayout packer4 = new IncrementalCirclePackingNodeLayout();

      packer4.oldLayout = packer3;

      root.AddChild(node4);
      packer4.Create(new[] { root, node1, node2, node3, node4 }, Vector3.zero, Vector2.one);
       
       */
      Debug.Log("2");
      Debug.Log(circle1.ToString());
      Debug.Log(circle2.ToString());


    }

    //*************************************************************************************************************
    [Test]
    public void TestOBvious()
    {
      Debug.Log((int)MathF.Floor(0.1f / 0.01f));
    }

  }
}

