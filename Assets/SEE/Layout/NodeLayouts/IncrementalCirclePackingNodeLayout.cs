using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{

  public class IncrementalCirclePackingNodeLayout : NodeLayout, IIncrementalNodeLayout
  {
    static IncrementalCirclePackingNodeLayout()
    {
      Name = "Incremental Circle Packing";
    }

    public IncrementalCirclePackingNodeLayout oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is IncrementalCirclePackingNodeLayout layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(IncrementalCirclePackingNodeLayout)} was not an {nameof(IncrementalCirclePackingNodeLayout)}.");
        }
      }
    }

    private Dictionary<ILayoutNode, NodeTransform> layoutResult;

    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;
    //********************************************************************************************************************************
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {

      return FirstScenario(layoutNodes, centerPosition, rectangle);

    }
    //********************************************************************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      layoutResult = new Dictionary<ILayoutNode, NodeTransform>();

      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
      if (roots.Count == 0)
      {
        throw new System.Exception("Graph has no root node.");
      }
      else if (roots.Count > 1)
      {
        throw new System.Exception("Graph has more than one root node.");
      }
      else
      {
        ILayoutNode root = roots.FirstOrDefault();

        //AddToHistory(layoutResult, layoutNodes.ToList(), rectangle, root.ID);

        // exactly one root
        float outRadius = PlaceNodes(root, layoutResult);
        Vector2 position = Vector2.zero;
        layoutResult[root] = new NodeTransform(position.x, position.y, GetScale(root, outRadius));
        MakeGlobal(layoutResult, position, root.Children());
        //Debug.Log("**************************************************************************************");
        return layoutResult;
      }
    }
    //********************************************************************************************************************************
    private static void MakeGlobal(Dictionary<ILayoutNode, NodeTransform> layoutResult, Vector2 position, ICollection<ILayoutNode> children)
    {
      foreach (ILayoutNode child in children)
      {
        NodeTransform childTransform = layoutResult[child];
        Vector2 childPosition = new Vector2(childTransform.X, childTransform.Z) + position;
        childTransform.MoveTo(childPosition.x, childPosition.y);
        layoutResult[child] = childTransform;
        MakeGlobal(layoutResult, childPosition, child.Children());
      }
    }

    //********************************************************************************************************************************
    public float PlaceNodes(ILayoutNode parent, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      ICollection<ILayoutNode> children = parent.Children();

      if (children.Count == 0)
      {

        return LeafRadius(parent);
      }
      else
      {
        List<Circle1> circles = new(children.Count);

        int i = 0;
        foreach (ILayoutNode child in children)
        {
          float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child, layout);

          float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
          circles.Add(new Circle1(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
          i++;
        }
        
        IncrementalCirclePacker.PackCircles(circles, Vector2.zero, out float outOuterRadius, oldLayout == null, parent.ID);

        if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
        {
          outOuterRadius *= 1.2f;
        }

        foreach (Circle1 circle in circles)
        {

          layout[circle.GameObject]
               = new NodeTransform(circle.Center.x, circle.Center.y,
                                   GetScale(circle.GameObject, circle.Radius));
        }
        return outOuterRadius;
      }
    }


    
    //********************************************************************************************************************************
    private static Vector3 GetScale(ILayoutNode node, float radius)
    {
      return node.IsLeaf ? node.AbsoluteScale
                         : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
    }

    //********************************************************************************************************************************
    private static float LeafRadius(ILayoutNode block)
    {
      Vector3 extent = block.AbsoluteScale / 2.0f;
      return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
    }
  }
}
