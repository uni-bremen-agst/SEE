using SEE.Layout.NodeLayouts.CirclePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// This layout packs circles closely together as a set of nested circles to decrease
  /// the total area of city.
  /// </summary>
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

    /// <summary>
    /// The node layout we compute as a result.
    /// </summary>
    private Dictionary<ILayoutNode, NodeTransform> layoutResult;

    /// <summary>
    /// See <see cref="NodeLayout.Layout"/>.
    /// </summary>
    /// <exception cref="Exception">Thrown if there is no root in <paramref name="gameNodes"/>
    /// or if there is more than one root.</exception>
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
        //MakeGlobal(layoutResult, position, root.Children());
        return layoutResult;
      }
    }

    /// <summary>
    /// "Globalizes" the layout. Initially, the position of children are assumed to be
    /// relative to their parent, where the parent has position (0, 0). This
    /// function adjusts the X/Z co-ordinates of all nodes to the world's co-ordinates.
    /// </summary>
    /// <param name="layoutResult">The layout to be adjusted.</param>
    /// <param name="position">The position of the parent of all children.</param>
    /// <param name="children">The children to be laid out.</param>
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

    /// <summary>
    /// Places all children of the given parent node (recursively for all descendants
    /// of the given parent).
    /// </summary>
    /// <param name="parent">Node whose descendants are to be placed.</param>
    /// <returns>The radius required for a circle represent parent.</returns>
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

    /// <summary>
    /// Returns the scaling vector for given node. If the node is a leaf, it will be the node's original size
    /// because leaves are not scaled at all. We do not want to change their size. Its predetermined
    /// by the client of this class. If the node is not a leaf, it will be represented by a circle
    /// whose scaling in x and y axes is twice the given radius (we do not scale along the y axis,
    /// hence, co-ordinate y of the resulting vector is always circleHeight.
    /// </summary>
    /// <param name="node">Game node whose size is to be determined.</param>
    /// <param name="radius">The radius for the game node if it is an inner node.</param>
    /// <returns>The scale of <paramref name="node"/>.</returns>
    private static Vector3 GetScale(ILayoutNode node, float radius)
    {
      return node.IsLeaf ? node.AbsoluteScale 
                         : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
    }

    /// <summary>
    /// Yields the radius of the minimal circle containing the given block.
    ///
    /// Precondition: node must be a leaf node, a block generated by blockFactory.
    /// </summary>
    /// <param name="block">Block whose radius is required.</param>
    /// <returns>Radius of the minimal circle containing the given block.</returns>
    private static float LeafRadius(ILayoutNode block)
    {
      Vector3 extent = block.AbsoluteScale / 2.0f;
      return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
    }
  }
}
