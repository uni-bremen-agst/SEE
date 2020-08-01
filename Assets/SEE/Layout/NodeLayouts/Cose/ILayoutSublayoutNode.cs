using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// ILayoutNode for the caluclation of the sublayouts
    /// </summary>
    public class ILayoutSublayoutNode : ILayoutNode
    {
        /// <summary>
        /// the scale
        /// </summary>
        public Vector3 LocalScale { get => node.LocalScale ; set => node.LocalScale = value; }

        /// <summary>
        /// the center position
        /// </summary>
        public Vector3 CenterPosition { get => node.CenterPosition; set => node.CenterPosition = value; }

        /// <summary>
        /// the rotation
        /// </summary>
        public float Rotation { get => node.Rotation; set => node.Rotation = value; }

        /// <summary>
        ///  the level
        /// </summary>
        public int Level { get => node.Level; set => node.Level = value; }

        /// <summary>
        /// the id 
        /// </summary>
        public string ID => node.ID;

        /// <summary>
        /// roof vector
        /// </summary>
        public Vector3 Roof => node.Roof;

        /// <summary>
        /// ground vector
        /// </summary>
        public Vector3 Ground => node.Ground;

        /// <summary>
        /// collection of successor nodes
        /// </summary>
        public ICollection<ILayoutNode> Successors => GetSuccessors(); 

        /// <summary>
        /// the parent node
        /// </summary>
        public ILayoutNode Parent => GetParent();

        /// <summary>
        /// true if node is a leaf node
        /// </summary>
        public bool IsLeaf => isLeaf;

        /// <summary>
        /// Returns all Children of a node 
        /// </summary>
        /// <returns></returns>
        public ICollection<ILayoutNode> Children()
        {
            return GetChildren();
        }

        /// <summary>
        /// the underlying ILayoutNode
        /// </summary>
        public ILayoutNode Node => node;

        /// <summary>
        /// the relative position of this node to its sublayout root
        /// </summary>
        public Vector3 RelativePosition { get; set; }

        /// <summary>
        ///  true if node is a sublayout node
        /// </summary>
        public bool IsSublayoutNode { get => false; set => value = false; }

        /// <summary>
        /// the if node is a sublayout root node
        /// </summary>
        public bool IsSublayoutRoot { get => false; set => value = false; }

        /// <summary>
        /// the sublayout of this node 
        /// </summary>
        public Sublayout Sublayout { get; set; }

        /// <summary>
        /// if node is a sublayoutNode this property holds the sublayout root node 
        /// </summary>
        public ILayoutNode SublayoutRoot { get; set; }

        public Vector3 AbsoluteScale => throw new System.NotImplementedException();

        /// <summary>
        /// the child nodes
        /// </summary>
        private ICollection<ILayoutNode> children;

        /// <summary>
        /// true if node is a leaf node
        /// </summary>
        private readonly bool isLeaf;

        /// <summary>
        ///  the underlying iLayoutnode
        /// </summary>
        private readonly ILayoutNode node;

        /// <summary>
        ///  the parent node
        /// </summary>
        private ILayoutNode parent;

        /// <summary>
        /// the successor nodes
        /// </summary>
        private ICollection<ILayoutNode> successors;

        /// <summary>
        /// holding the child nodes temporary 
        /// </summary>
        private readonly ICollection<ILayoutNode> temporaryChildren;

        /// <summary>
        /// holding the parent node temporary
        /// </summary>
        private readonly ILayoutNode temporaryParent;

        /// <summary>
        /// a dictinary holding a mapping from the underlying IlayoutNode to a CoseSublayoutNode
        /// </summary>
        private readonly Dictionary<ILayoutNode, ILayoutSublayoutNode> ILayout_to_CoseSublayoutNode;

        public ILayoutSublayoutNode(ILayoutNode node, ICollection<ILayoutNode> children, bool isLeaf, ILayoutNode parent, Vector3 localScale, Dictionary<ILayoutNode, ILayoutSublayoutNode> ILayout_to_CoseSublayoutNode)
        {
            this.node = node; 
            this.isLeaf = isLeaf;
            this.temporaryChildren = children;
            this.temporaryParent = parent;
            node.LocalScale = localScale;
            this.ILayout_to_CoseSublayoutNode = ILayout_to_CoseSublayoutNode;
            ILayout_to_CoseSublayoutNode[node] = this; 
        }

        public ILayoutSublayoutNode(ILayoutNode node, Dictionary<ILayoutNode, ILayoutSublayoutNode> ILayout_to_CoseSublayoutNode)
        {
            this.node = node;
            this.isLeaf = node.IsLeaf;
            this.temporaryChildren = node.Children();
            this.temporaryParent = node.Parent;
            node.LocalScale = node.LocalScale;
            this.ILayout_to_CoseSublayoutNode = ILayout_to_CoseSublayoutNode;
            ILayout_to_CoseSublayoutNode[node] = this;
        }

        private ILayoutNode GetParent()
        {
            if (parent == null && temporaryParent != null)
            {
                if (ILayout_to_CoseSublayoutNode.ContainsKey(temporaryParent))
                {
                    parent = ILayout_to_CoseSublayoutNode[temporaryParent];
                }
            }
            return parent; 
        }

        private ICollection<ILayoutNode> GetSuccessors()
        {
            if (successors == null)
            {
                ICollection<ILayoutNode> succesorsCollection = new List<ILayoutNode>();
                foreach (ILayoutNode successor in node.Successors)
                {
                    if (ILayout_to_CoseSublayoutNode.ContainsKey(successor))
                    {
                        succesorsCollection.Add(ILayout_to_CoseSublayoutNode[successor]);
                    } 
                }
                successors = succesorsCollection;
            }
            return successors;
        }

        private ICollection<ILayoutNode> GetChildren()
        {
            if (children == null)
            {
                List<ILayoutNode> childrenList = new List<ILayoutNode>();

                foreach (ILayoutNode child in temporaryChildren)
                {
                    if (ILayout_to_CoseSublayoutNode.ContainsKey(child))
                    {
                        childrenList.Add(ILayout_to_CoseSublayoutNode[child]);
                    }
                }
                children = childrenList;
            }
            return children;
        }

        public void SetOrigin()
        {
            throw new System.NotImplementedException();
        }

        public void SetRelative(ILayoutNode node)
        {
            throw new System.NotImplementedException();
        }

        public void ScaleBy(float factor)
        {
            throw new System.NotImplementedException();
        }
    }
}

