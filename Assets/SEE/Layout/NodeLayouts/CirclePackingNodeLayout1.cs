using SEE.Layout.NodeLayouts.CirclePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{

  public class CirclePackingNodeLayout1 : NodeLayout, IIncrementalNodeLayout
  {
    static CirclePackingNodeLayout1()
    {
      Name = "Circle Packing";
    }

    public CirclePackingNodeLayout1 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is CirclePackingNodeLayout1 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(CirclePackingNodeLayout1)} was not an {nameof(CirclePackingNodeLayout1)}.");
        }
      }
    }

    private Dictionary<ILayoutNode, NodeTransform> layoutResult;

    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;

    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
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
        // exactly one root
        ILayoutNode root = roots.FirstOrDefault();
        float outRadius = PlaceNodes(root);
        Vector2 position = Vector2.zero;
        layoutResult[root] = new NodeTransform(position.x, position.y, GetScale(root, outRadius));
        //MakeGlobal(layoutResult, position, root.Rests());
        return layoutResult;
      }
    }

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

    private float PlaceNodes(ILayoutNode parent)
    {
      ICollection<ILayoutNode> children = parent.Children();

      if (children.Count == 0)
      {
        /*
        // No scaling for leaves because they are already scaled.
        // Position Vector3.zero because they are located relative to their parent.
        // This position may be overridden later in the context of parent's parent.
         */
        return LeafRadius(parent);
      }
      else
      {
        List<Circle> circles = new(children.Count);

        int i = 0;
        foreach (ILayoutNode child in children)
        {
          float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child);
          // Position the children on a circle as required by CirclePacker.Pack.
          float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
          circles.Add(new Circle(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
          i++;
        }
        /*
        // The co-ordinates returned in circles are local, that is, relative to the zero center.
        // The resulting center and outOuterRadius relate to the circle object comprising
        // the children we just packed. By necessity, the objects whose children we are
        // currently processing is a composed object represented by a circle, otherwise
        // we would not have any children here.
         */
        CirclePacker.Pack(0.1f, circles, out float outOuterRadius);

        if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
        {
          outOuterRadius *= 1.2f;
        }

        foreach (Circle circle in circles)
        {
          /*
          // Note: The position of the transform is currently only local, relative to the zero center
          // within the parent node. The co-ordinates will later be adjusted to the world scope.
           */
          layoutResult[circle.GameObject] = new NodeTransform(circle.Center.x, circle.Center.y, GetScale(circle.GameObject, circle.Radius));
        }
        return outOuterRadius;
      }
    }

    private static Vector3 GetScale(ILayoutNode node, float radius)
    {
      return node.IsLeaf ? node.AbsoluteScale 
                         : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
    }

    private static float LeafRadius(ILayoutNode block)
    {
      Vector3 extent = block.AbsoluteScale / 2.0f;
      return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
    }
  }
}
