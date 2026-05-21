using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// This class holds the necessary information for a mind map node.
    /// </summary>
    public class MMNodeValueHolder : MonoBehaviour
    {
        /// <summary>
        /// Property for the node kind of the mind map node.
        /// </summary>
        public GameMindMap.NodeKind NodeKind { get; set; }

        /// <summary>
        /// The layer of the mind map node.
        /// Will be needed to load a mind map from file.
        /// </summary>
        private int layer;

        /// <summary>
        /// The property for the node layer.
        /// </summary>
        public int Layer
        {
            get { return layer; }
            set
            {
                if (NodeKind == GameMindMap.NodeKind.Theme)
                {
                    layer = 0;
                }
                else
                {
                    layer = value;
                }
            }
        }

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
        /// The children of this node.
        /// Is empty if the node is a leaf.
        /// </summary>
        private IDictionary<GameObject, GameObject> children;

        /// <summary>
        /// Initializes the properties.
        /// </summary>
        private void Awake()
        {
            children = new Dictionary<GameObject, GameObject>();
            if (gameObject.name.StartsWith(ValueHolder.MindMapThemePrefix))
            {
                NodeKind = GameMindMap.NodeKind.Theme;
            }
            else if (gameObject.name.StartsWith(ValueHolder.MindMapSubthemePrefix))
            {
                NodeKind = GameMindMap.NodeKind.Subtheme;
            }
            else
            {
                NodeKind = GameMindMap.NodeKind.Leaf;
            }
            layer = 0;
        }

        /// <summary>
        /// Sets the parent.
        /// If the node kind is a theme, the parent will be null.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        public void SetParent(GameObject parent, GameObject branchLine)
        {
            if (NodeKind == GameMindMap.NodeKind.Theme)
            {
                if (this.parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().RemoveChild(gameObject);
                }
                this.parent = null;
                branchLineToParent = null;
            }
            else
            {
                this.parent = parent;
                branchLineToParent = branchLine;
                if (parent != null)
                {
                    parent.GetComponent<MMNodeValueHolder>().AddChild(gameObject, branchLine);
                }
            }
        }

        /// <summary>
        /// Gets the parent. Can be null
        /// </summary>
        /// <returns>The parent.</returns>
        public GameObject GetParent()
        {
            return parent;
        }

        /// <summary>
        /// Gets the branch line to the parent node.
        /// </summary>
        /// <returns>Parent branch line.</returns>
        public GameObject GetParentBranchLine()
        {
            return branchLineToParent;
        }

        /// <summary>
        /// Get the dictionary of the children.
        /// It contains the child as key and as value the branch line.
        /// </summary>
        /// <returns>The child dictionary.</returns>
        public IDictionary<GameObject, GameObject> GetChildren()
        {
            return children;
        }

        /// <summary>
        /// Gets a dictionary of all children.
        /// Includes also the child's children recursively up to and including the lowest child.
        /// </summary>
        /// <returns>A dictionary with all children and their branch lines of this nodes.</returns>
        public IDictionary<GameObject, GameObject> GetAllChildren()
        {
            IDictionary<GameObject, GameObject> children = new Dictionary<GameObject, GameObject>(this.children);
            foreach (KeyValuePair<GameObject, GameObject> pair in this.children)
            {
                MMNodeValueHolder vH = pair.Key.GetComponent<MMNodeValueHolder>();
                foreach (KeyValuePair<GameObject, GameObject> child in vH.GetAllChildren())
                {
                    children.Add(child.Key, child.Value);
                }
            }
            return children;
        }

        /// <summary>
        /// Gets a list with all parent ancestors
        /// </summary>
        /// <returns>A list that contains all parent ancestors.</returns>
        public List<GameObject> GetAllParentAncestors()
        {
            List<GameObject> parents = new();
            if (parent != null)
            {
                parents.Add(parent);
                parents.AddRange(parent.GetComponent<MMNodeValueHolder>().GetAllParentAncestors());
            }
            return parents;
        }

        /// <summary>
        /// Adds a new key value to the children dictionary.
        /// </summary>
        /// <param name="childNode">The child node, will used as key.</param>
        /// <param name="branchLine">The branch line, will used as value.</param>
        public void AddChild(GameObject childNode, GameObject branchLine)
        {
            if (!children.ContainsKey(childNode))
            {
                children.Add(childNode, branchLine);
            }
        }

        /// <summary>
        /// Removes a child from the dictionary.
        /// </summary>
        /// <param name="child">The child that should be removed.</param>
        public void RemoveChild(GameObject child)
        {
            if (children.ContainsKey(child))
            {
                children.Remove(child);
            }
        }
    }
}