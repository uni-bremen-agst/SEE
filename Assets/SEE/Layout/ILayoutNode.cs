using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public interface IGameNode
    {
        /// <summary>
        /// A unique ID for a node.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The local scale of a node (i.e., scale relative to its parent).
        /// </summary>
        Vector3 LocalScale { get; set; }

        /// <summary>
        /// The absolute scale of a node in world co-ordinates.
        /// 
        /// Note: This value may be meaningful only if the node is not skewed.
        /// </summary>
        Vector3 AbsoluteScale { get; }

        /// <summary>
        /// Scales the node by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="factor">factory by which to scale the node</param>
        void ScaleBy(float factor);

        /// <summary>
        /// Center position of a node in world space.
        /// </summary>
        Vector3 CenterPosition { get; set; }

        /// <summary>
        /// Rotation around the y axis in degrees.
        /// </summary>
        float Rotation { get; set; }

        /// <summary>
        /// X-Z center position of the roof of the node in world space.
        /// </summary>
        Vector3 Roof { get; }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        Vector3 Ground { get; }
    }

    public interface IGraphNode<T>
    {
        /// <summary>
        /// The set of immediate successors of this node.
        /// </summary>
        ICollection<T> Successors { get; }
    }

    public interface ISublayoutNode<T>
    {
        /// <summary>
        /// the relative position from a sublayoutNode to its sublayoutRoot node
        /// </summary>
        Vector3 RelativePosition { get; set; }

        /// <summary>
        /// true if node is a sublayouNode
        /// </summary>
        bool IsSublayoutNode { get; set; }

        /// <summary>
        /// true if node is a root node of a sublayout
        /// </summary>
        bool IsSublayoutRoot { get; set; }

        /// <summary>
        /// if the node is a sublayout root, this is the sublayout 
        /// </summary>
        Sublayout Sublayout { get; set; }

        /// <summary>
        /// the sublayout root node
        /// </summary>
        T SublayoutRoot { get; set; }

        void SetOrigin();

        void SetRelative(T node);
    }

    /// <summary>
    ///  Defines the methods for all nodes to be laid out.
    /// </summary>
    public interface ILayoutNode : IGameNode, IGraphNode<ILayoutNode>, IHierarchyNode<ILayoutNode>, ISublayoutNode<ILayoutNode>
    {
    }

    public static class ILayoutNodeHierarchy
    {
        /// <summary>
        /// Returns all nodes in <paramref name="layoutNodes"/> that do not have a parent.
        /// </summary>
        /// <param name="layoutNodes">nodes to be queried</param>
        /// <returns>all root nodes in <paramref name="layoutNodes"/></returns>
        public static ICollection<ILayoutNode> Roots(ICollection<ILayoutNode> layoutNodes)
        {
            ICollection<ILayoutNode> result = new List<ILayoutNode>();
            foreach (ILayoutNode node in layoutNodes)
            {
                if (node.Parent == null)
                {
                    result.Add(node);
                }
            }
            return result;
        }
    }
}