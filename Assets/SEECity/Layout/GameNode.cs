using System.Collections.Generic;
using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Implementation of LayoutNode. It is simply a wrapper to game objects
    /// created for inner nodes or leaf nodes.
    /// </summary>
    public class GameNode : LayoutNode
    {
        private readonly GameObject gameObject;
        private readonly NodeFactory leafNodeFactory;
        private readonly Node node;
        private readonly Dictionary<Node, GameNode> to_layout_node = new Dictionary<Node, GameNode>();

        public LayoutNode Parent
        {
            get => node.Parent != null ? to_layout_node[node.Parent] : null;
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public Node GetNode()
        {
            return node;
        }

        public GameNode(Dictionary<Node, GameNode> to_layout_node, GameObject gameObject, NodeFactory nodeFactory)
        {
            this.gameObject = gameObject;
            this.leafNodeFactory = nodeFactory;
            this.node = this.gameObject.GetComponent<NodeRef>().node;
            this.to_layout_node = to_layout_node;
            to_layout_node[node] = this;
        }

        public GameNode(Dictionary<Node, GameNode> to_layout_node, GameObject gameObject)
            : this(to_layout_node, gameObject, null)
        {
        }

        public Vector3 GetSize()
        {
            if (node.IsLeaf())
            {
                return leafNodeFactory.GetSize(gameObject);
            }
            else
            {
                return gameObject.transform.localScale;
            }
        }

        public bool IsLeaf()
        {
            return node.IsLeaf();
        }

        public string LinkName()
        {
            return node.LinkName;
        }

        public IList<LayoutNode> Children()
        {
            IList<LayoutNode> children = new List<LayoutNode>();
            foreach (Node node in node.Children())
            {
                children.Add(to_layout_node[node]);
            }
            return children;
        }
    }
}