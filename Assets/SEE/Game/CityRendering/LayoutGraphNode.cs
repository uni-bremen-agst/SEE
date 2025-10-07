using SEE.DataModel.DG;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game.CityRendering
{
  /// <summary>
  /// Implementation of ILayoutNode wrapping a graph node and its layout
  /// information. It is used by the EvolutionRenderer in the upfront calculation
  /// of layouts when the layout information should not be applied immediately to
  /// game objects.
  /// </summary>
  public class LayoutGraphNode : AbstractLayoutNode
  {
    /// <summary>
    /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node.
    /// </summary>
    /// <param name="node">graph node corresponding to this layout node</param>
    public LayoutGraphNode(Node node)
        : base(node)
    { }

    /// <summary>
    /// The scale of the node (both local and absolute scale; there is
    /// no distinction here).
    /// </summary>
    private Vector3 scale;

    /// <summary>
    /// <see cref="AbstractLayoutNode.LocalScale"/>.
    /// </summary>
    public override Vector3 AbsoluteScale
    {
      get => scale;
      set => scale = value;
    }

    /// <summary>
    /// <see cref="AbstractLayoutNode.ScaleXZBy"/>.
    /// </summary>
    public override void ScaleXZBy(float factor)
    {
      scale.x *= factor;
      scale.z *= factor;
    }

    /// <summary>
    /// The position of the center of the node.
    /// </summary>
    private Vector3 centerPosition;

    /// <summary>
    /// <see cref="AbstractLayoutNode.CenterPosition"/>.
    /// </summary>
    public override Vector3 CenterPosition
    {
      get => centerPosition;
      set => centerPosition = value;
    }

    /// <summary>
    /// The rotation of the node along the y axis.
    /// </summary>
    private float rotation;

    /// <summary>
    /// <see cref="AbstractLayoutNode.Rotation"/>.
    /// </summary>
    public override float Rotation
    {
      get => rotation;
      set => rotation = value;
    }

    /// <summary>
    /// <see cref="AbstractLayoutNode.Roof"/>.
    /// </summary>
    public override Vector3 Roof
    {
      get => centerPosition + 0.5f * scale.y * Vector3.up;
    }

    /// <summary>
    /// <see cref="AbstractLayoutNode.Ground"/>.
    /// </summary>
    public override Vector3 Ground
    {
      get => centerPosition - 0.5f * scale.y * Vector3.up;
    }

    /// <summary>
    /// Creates a deep clone of this LayoutGraphNode, including its layout data and children.
    /// </summary>
    /// 

    /*
     
    public override object Clone()
    {
      // Create new instance with same underlying graph node
      var clone = new LayoutGraphNode(this.Node) // assuming AbstractLayoutNode exposes "Node"
      {
        AbsoluteScale = this.AbsoluteScale,
        CenterPosition = this.CenterPosition,
        Rotation = this.Rotation,
        Level = this.Level
      };

      // Clone children recursively
      foreach (var child in this.Children())
      {
        clone.AddChild((ILayoutNode)child.Clone());
      }

      return clone;
    }
     */
  }
}