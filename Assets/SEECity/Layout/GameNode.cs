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

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public Node GetNode()
        {
            return node;
        }

        public GameNode(GameObject gameObject, NodeFactory nodeFactory)
        {
            this.gameObject = gameObject;
            this.leafNodeFactory = nodeFactory;
            this.node = this.gameObject.GetComponent<NodeRef>().node;
        }

        public GameNode(GameObject gameObject)
        {
            this.gameObject = gameObject;
            this.leafNodeFactory = null;
            this.node = this.gameObject.GetComponent<NodeRef>().node;
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
            return gameObject.LinkName();
        }
    }
}