using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class holds the necessary information for a mind map node.
    /// </summary>
    public class MMNodeValueHolder : MonoBehaviour
    {
        /// <summary>
        /// The kind of the mind map node. (Theme/Subtheme/Leaf)
        /// </summary>
        private GameMindMap.NodeKind nodeKind;
        /// <summary>
        /// The layer of the mind map node.
        /// Will needed for load a mind map from file.
        /// </summary>
        private int layer;
        /// <summary>
        /// The parent of this node.
        /// Is null when the node is a theme.
        /// </summary>
        private GameObject parent;
        /// <summary>
        /// The branch line to the parent node.
        /// Is null when the node is a theme.
        /// </summary>
        private GameObject branchLineToParent;
        /// <summary>
        /// The childs of this node.
        /// Is empty if the node is a leaf.
        /// </summary>
        private Dictionary<GameObject, GameObject> childs;

        /// <summary>
        /// Initialized the dictionary and the node kind.
        /// </summary>
        private void Awake()
        {
            childs = new Dictionary<GameObject, GameObject>();
            if (gameObject.name.StartsWith(ValueHolder.MindMapThemePrefix))
            {
                this.nodeKind = GameMindMap.NodeKind.Theme;
            } else if (gameObject.name.StartsWith(ValueHolder.MindMapSubthemePrefix))
            {
                this.nodeKind = GameMindMap.NodeKind.Subtheme;
            } else
            {
                this.nodeKind = GameMindMap.NodeKind.Leaf;
            }
            layer = 0;
        }

        /// <summary>
        /// Gets the node kind
        /// </summary>
        /// <returns>The node kind</returns>
        public GameMindMap.NodeKind GetNodeKind()
        {
            return nodeKind;
        }

        /// <summary>
        /// Sets a new node kind.
        /// </summary>
        /// <param name="nodeKind">The new node kind for the node.</param>
        public void SetNodeKind(GameMindMap.NodeKind nodeKind)
        {
            this.nodeKind = nodeKind;
        }

        /// <summary>
        /// Sets the node layer.
        /// </summary>
        /// <param name="layer">node layer</param>
        public void SetLayer(int layer)
        {
            if (nodeKind == GameMindMap.NodeKind.Theme)
            {
                this.layer = 0;
            } else
            {
                this.layer = layer;
            }
        }

        /// <summary>
        /// Gets the node layer
        /// </summary>
        /// <returns>node layer</returns>
        public int GetLayer()
        {
            return layer;
        }

        /// <summary>
        /// Sets the parent.
        /// If the node kind is a theme, the parent will be null.
        /// </summary>
        /// <param name="parent">The parent node</param>
        public void SetParent(GameObject parent, GameObject branchLine)
        {
            if (nodeKind == GameMindMap.NodeKind.Theme)
            {
                if (this.parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().RemoveChild(this.gameObject);
                }
                this.parent = null;
                this.branchLineToParent = null;
            } else
            {
                this.parent = parent;
                this.branchLineToParent = branchLine;
                if (parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().AddChild(this.gameObject, branchLine);
                }
            }
        }

        /// <summary>
        /// Gets the parent. Can be null
        /// </summary>
        /// <returns>The parent</returns>
        public GameObject GetParent()
        {
            return parent;
        }

        /// <summary>
        /// Gets the branch line to the parent node.
        /// </summary>
        /// <returns>parent branch line</returns>
        public GameObject GetParentBranchLine()
        {
            return branchLineToParent;
        }

        /// <summary>
        /// Get the dictionary of the children.
        /// It contains the child as key and as value the branch line.
        /// </summary>
        /// <returns>the child dictionary</returns>
        public Dictionary<GameObject, GameObject> GetChildren()
        {
            return childs;
        }

        /// <summary>
        /// Gets a dictionary of all children.
        /// "Includes also the child's children recursively up to and including the lowest child.
        /// </summary>
        /// <returns></returns>
        public Dictionary<GameObject, GameObject> GetAllChildren()
        {
            Dictionary<GameObject, GameObject> children = new(childs);
            foreach(KeyValuePair<GameObject, GameObject> pair in childs)
            {
                MMNodeValueHolder vH = pair.Key.GetComponent<MMNodeValueHolder>();
                foreach(KeyValuePair<GameObject, GameObject> child in vH.GetAllChildren())
                {
                    children.Add(child.Key, child.Value);
                }
            }
            return children;
        }

        /// <summary>
        /// Adds a new key value to the children dictionary.
        /// </summary>
        /// <param name="childNode">The child node, will used as key.</param>
        /// <param name="branchLine">The branch line, will used as value.</param>
        public void AddChild(GameObject childNode, GameObject branchLine)
        {
            if (!childs.ContainsKey(childNode))
            {
                childs.Add(childNode, branchLine);
            }
        }

        /// <summary>
        /// Removes a child from the dictionary.
        /// </summary>
        /// <param name="child">The child that should be removed.</param>
        public void RemoveChild(GameObject child)
        {
            if (childs.ContainsKey(child))
            {
                childs.Remove(child);
            }
        }
    }
}