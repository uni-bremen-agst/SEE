using SEE.Game.CityRendering;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE.DataModel.DG;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// Simple rectangle layout that places nodes in a line
  /// and sorts them descending by Z inside the rectangle.
  /// </summary>
  public class ZSortedRectangleLayout : NodeLayout
  {
    static ZSortedRectangleLayout()
    {
      Name = "ZSortedRectangleLayout";
    }

    //protected override LayoutAnchor Anchor => LayoutAnchor.TopLeft;

    public List<Rec> recs;
    public List<ILayoutNode> leafsNodes;
    public Dictionary<ILayoutNode, NodeTransform> entries;
    LayoutGraphNode rootLayoutNode;
    Graph graph;
    Node rootNode;

    /*
    public override Dictionary<ILayoutNode, NodeTransform> Create(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      var layout = base.Create(layoutNodes, centerPosition, rectangle);

      if (layout.Count == 0)
        return layout;

      var entry = layout.First();
      NodeTransform t = entry.Value;

      float x = centerPosition.x - rectangle.x / 2f + t.Scale.x / 2f;
      float z = centerPosition.z + rectangle.y / 2f - t.Scale.z / 2f;

      t.MoveTo(x, z);

      return layout;
    }
     */
    /*
    what is my task today
    make a rectangle of the size of the node
    place the node in the middle of it
    place the next rectangle next to it until the limit z is reached
    then go to the next row


     */
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      graph = new Graph();
      rootNode = new Node();
      entries = new Dictionary<ILayoutNode, NodeTransform>();
      recs = new List<Rec>();
      leafsNodes = layoutNodes.Where(n => n != null && n.IsLeaf).ToList();

      //FirstScenario(layoutNodes, centerPosition, rectangle);
      SecondScenario(leafsNodes, centerPosition, rectangle);

      PrintDictionary(entries);
      
      return entries;

    }

    public void SecondScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      SetRootLayoutNode(rectangle);
      float zStart = centerPosition.z + rectangle.y / 2f;
      float xStart = centerPosition.z + rectangle.y / 2f;

      float zPointer = zStart;
      float xPointer = xStart;

      float zScalePointer = zStart;
      float xScalePointer = xStart;

      float limitZ = -zStart;
      float limitX = -xStart;

      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      entries[rootLayoutNode] = parentTransform;

      foreach (var leafNode in leafsNodes)
      {
        Vector3 nodeScale = leafNode.AbsoluteScale;
        if (zPointer + nodeScale.x > limitZ)
        {
          // Move to next row
          zPointer = zStart;
          xPointer -= xScalePointer;
        }
        NodeTransform nodeTransform = new NodeTransform(
            zPointer,
            xPointer,
            nodeScale
        );
        if (nodeScale.z > xScalePointer) xScalePointer = nodeScale.z;
        entries[leafNode] = nodeTransform;
        xPointer -= nodeScale.x + .1f;

        rootLayoutNode.AddChild(leafNode);
        
      }
    }
    public void PlaceNodesInRecs()
    {
      foreach (var leafNode in leafsNodes)
      {
        Rec nodeRec = new Rec(0, 0, leafNode.AbsoluteScale.x, leafNode.AbsoluteScale.z);
      }
    }
    public void PrintDictionary(Dictionary<ILayoutNode, NodeTransform> dict)
      {
        foreach (var entry in dict)
        {
          Debug.Log($"Node: {entry.Key.Print()}, Transform: {entry.Value}");
        }
      }
    public void FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      ILayoutNode firstNode = layoutNodes.FirstOrDefault(n => n != null && n.IsLeaf);

      SetRootLayoutNode(rectangle);

      Debug.Log(rectangle + " " + centerPosition);

      /*
      for (int i = 0; i < count; i++)
      {
        ILayoutNode node = nodes[i];

        float z = startZ - i * spacing;

      }

      float xx = x - rootLayoutNode.AbsoluteScale.x / 2f;
      float zz = z - rootLayoutNode.AbsoluteScale.z / 2f;
       */
      float x = centerPosition.x - rectangle.x / 2f;
      float z = centerPosition.z + rectangle.y / 2f;


      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      NodeTransform firstNodeTransform = new NodeTransform(
          -z,
          -z,
          firstNode.AbsoluteScale
      );

      entries[rootLayoutNode] = parentTransform;
      entries[firstNode] = firstNodeTransform;

    }

    public void SetRootLayoutNode(Vector2 rectangle)
    {
      rootNode.ID = "1";
      graph.AddNode(rootNode);
      rootNode.ItsGraph = graph;
      rootLayoutNode = new LayoutGraphNode(rootNode);
      //rootLayoutNode.AddChild(firstNode);
      rootLayoutNode.Parent = null;

      rootLayoutNode.AbsoluteScale = new Vector3(rectangle.x * 2f, 0, rectangle.y * 2f);
    }


  }
  public class Rec
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="x">X co-ordinate at corner.</param>
    /// <param name="z">Z co-ordinate at corner.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="depth">Depth (breadth) of the rectangle.</param>
    public Rec(float x, float z, float width, float depth)
    {
      X = x;
      Z = z;
      Width = width;
      Depth = depth;
    }
    /// <summary>
    /// X co-ordinate at corner.
    /// </summary>
    public float X;
    /// <summary>
    /// Z co-ordinate at corner.
    /// </summary>
    public float Z;
    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width;
    /// <summary>
    /// Depth (breadth) of the rectangle.
    /// </summary>
    public float Depth;

    public Vector2 Center()
    {
      return new Vector2(X + Width / 2f, Z + Depth / 2f);
    }

    public Vector2 position
    {
      get { return new Vector2(X, Z); }
    }
  }
  public class NodeSize
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="gameNode">Layout node this node size corresponds to.</param>
    /// <param name="size">Size of the node.</param>
    public NodeSize(ILayoutNode gameNode, float size)
    {
      GameNode = gameNode;
      Size = size;
    }
    /// <summary>
    /// The layout node this node size corresponds to.
    /// </summary>
    public ILayoutNode GameNode;
    /// <summary>
    /// The size of the node.
    /// </summary>
    public float Size;
  }
}
