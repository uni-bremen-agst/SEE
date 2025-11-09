using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
  /// <summary>
  /// Defines the methods for all nodes to be laid out.
  /// </summary>
  public abstract class ILayoutNode : IGameNode, IHierarchyNode<ILayoutNode>
  {
    /// <summary>
    /// See <see cref="IGameNode.ID"/>.
    /// </summary>
    public abstract string ID { get; }

    /// <summary>
    /// See <see cref="IGameNode.AbsoluteScale"/>.
    /// </summary>
    public abstract Vector3 AbsoluteScale { get; set; }
    /// <summary>
    /// See <see cref="IGameNode.CenterPosition"/>.
    /// </summary>
    public abstract Vector3 CenterPosition { get; set; }
    /// <summary>
    /// See <see cref="IGameNode.Rotation"/>.
    /// </summary>
    public abstract float Rotation { get; set; }
    /// <summary>
    /// See <see cref="IGameNode.Roof"/>.
    /// </summary>
    public abstract Vector3 Roof { get; }
    /// <summary>
    /// See <see cref="IGameNode.Ground"/>.
    /// </summary>
    public abstract Vector3 Ground { get; }
    /// <summary>
    /// See <see cref="IGameNode.HasType"/>.
    /// </summary>
    public abstract bool HasType(string typeName);
    /// <summary>
    /// See <see cref="IGameNode.ScaleXZBy"/>.
    /// </summary>
    public abstract void ScaleXZBy(float factor);

    /// <summary>
    /// See <see cref="IHierarchyNode.Parent"/>.
    /// </summary>
    public ILayoutNode Parent { get; set; }

    /// <summary>
    /// See <see cref="IHierarchyNode.Parent"/>.
    /// </summary>
    public int Level { get; set; } = 0;

    /// <summary>
    /// List of children of this node.
    /// </summary>
    private readonly List<ILayoutNode> children = new();

    /// <summary>
    /// See <see cref="IHierarchyNode.IsLeaf"/>.
    /// </summary>
    public bool IsLeaf => children.Count == 0;

    /// <summary>
    /// See <see cref="IHierarchyNode.Children"/>.
    /// </summary>
    public ICollection<ILayoutNode> Children()
    {
      return children;
    }

    /// <summary>
    /// See <see cref="IHierarchyNode.AddChild(T)"/>.
    /// </summary>
    public void AddChild(ILayoutNode child)
    {
      children.Add(child);
      child.Parent = this;
    }

    /// <summary>
    /// See <see cref="IHierarchyNode.RemoveChild(T)"/>.
    /// </summary>
    public void RemoveChild(ILayoutNode child)
    {
      if (!children.Remove(child))
      {
        throw new System.Exception("Child not found.");
      }
      child.Parent = null;
    }


    /*
     
    public abstract object Clone();
     */
  
  }
}
