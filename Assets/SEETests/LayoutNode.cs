using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A simple implementation of ILayoutNode.
    /// </summary>
    internal class LayoutNode : ILayoutNode
    {
        public LayoutNode(Vector3 initialSize, int index)
        {
            this.scale = initialSize;
            this.id = index.ToString();
        }

        public LayoutNode(string id)
        {
            this.scale = Vector3.zero;
            this.id = id;
        }

        private readonly string id;

        private ILayoutNode parent;

        public ILayoutNode Parent
        {
            get => parent;
            set => parent = value;
        }

        private int level = 0;

        public int Level
        {
            get => level;
            set => level = value;
        }

        private List<ILayoutNode> children = new List<ILayoutNode>();

        public ICollection<ILayoutNode> Children()
        {
            return children;
        }

        public void AddChild(LayoutNode node)
        {
            children.Add(node);
            node.Parent = this;
        }

        private Vector3 scale;

        public Vector3 LocalScale
        {
            get => scale;
            set => scale = value;
        }

        public void ScaleBy(float factor)
        {
            scale *= factor;
        }

        private Vector3 centerPosition;

        public Vector3 CenterPosition
        {
            get => centerPosition;
            set => centerPosition = value;
        }

        public Vector3 Roof
        {
            get => centerPosition + Vector3.up * 0.5f * scale.y;
        }

        public Vector3 Ground
        {
            get => centerPosition - Vector3.up * 0.5f * scale.y;
        }

        public bool IsLeaf => children.Count == 0;

        public string ID { get => id; }

        private float rotation;

        public float Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        public ICollection<ILayoutNode> Successors => new List<ILayoutNode>();

        public Vector3 AbsoluteScale => scale;

        public static ICollection<ILayoutNode> CreateNodes()
        {
            int howManyNodes = 500;
            Vector3 initialSize = Vector3.one;
            LayoutNode root = new LayoutNode(initialSize, 0);

            ICollection<ILayoutNode> gameObjects = new List<ILayoutNode>();
            gameObjects.Add(root);

            for (int i = 1; i <= howManyNodes; i++)
            {
                initialSize *= 1.01f;
                LayoutNode child = new LayoutNode(initialSize, i);
                gameObjects.Add(child);
                root.AddChild(child);
            }
            return gameObjects;
        }
    }
}