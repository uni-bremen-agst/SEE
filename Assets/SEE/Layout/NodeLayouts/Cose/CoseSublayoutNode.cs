using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Layout
{
    public class CoseSublayoutNode : ILayoutNode
    {
        public Vector3 Scale { get => node.Scale ; set => node.Scale = value; }
        public Vector3 CenterPosition { get => node.CenterPosition; set => node.CenterPosition = value; }
        public float Rotation { get => node.Rotation; set => node.Rotation = value; }
        public int Level { get => node.Level; set => node.Level = value; }
        public string ID => node.ID;
        public Vector3 Roof => node.Roof;
        public Vector3 Ground => node.Ground;
        public ICollection<ILayoutNode> Successors => GetSuccessors(); 
        public ILayoutNode Parent => GetParent();
        public bool IsLeaf => isLeaf;
        public ICollection<ILayoutNode> Children()
        {
            return GetChildren();
        }
        public ILayoutNode Node => node;

        public Vector3 RelativePosition { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsSublayoutNode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsSublayoutRoot { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Sublayout Sublayout { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public ILayoutNode SublayoutRoot { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        private ICollection<ILayoutNode> children;
        private readonly bool isLeaf;
        private readonly ILayoutNode node;
        private ILayoutNode parent;
        private ICollection<ILayoutNode> successors;
        private readonly ICollection<ILayoutNode> temporaryChildren;
        private readonly ILayoutNode temporaryParent;

        private readonly Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode;

        public CoseSublayoutNode(ILayoutNode node, ICollection<ILayoutNode> children, bool isLeaf, ILayoutNode parent, Vector3 scale, Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode)
        {
            this.node = node; 
            this.isLeaf = isLeaf;
            this.temporaryChildren = children;
            this.temporaryParent = parent;
            node.Scale = scale;
            this.ILayout_to_CoseSublayoutNode = ILayout_to_CoseSublayoutNode;
            ILayout_to_CoseSublayoutNode[node] = this; 
        }

        public CoseSublayoutNode(ILayoutNode node, Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode)
        {
            this.node = node;
            this.isLeaf = node.IsLeaf;
            this.temporaryChildren = node.Children();
            this.temporaryParent = node.Parent;
            node.Scale = node.Scale;
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
    }
}

