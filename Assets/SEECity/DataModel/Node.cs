using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Node of a graph.
    /// </summary>
    [System.Serializable]
    public class Node : GraphElement
    {
        // Important note: Nodes should be created only by calling Graph.newNode().
        // Do not use 'new Node()'.

        private const string linknameAttribute = "Linkage.Name";

        /// <summary>
        /// The unique identifier of a node.
        /// </summary>
        [SerializeField]
        public string LinkName
        {
            get => GetString(linknameAttribute);
            // This will only set the linkname attribute, but does not alter the
            // hashed linknames of the underlying graph. You will likely want to
            // use Graph.SetLinkname instead. Otherwise expect inconsistencies.
            // This setter should only be called by Graph.SetLinkname.
            set => SetString(linknameAttribute, value);
        }

        private const string sourcenameAttribute = "Source.Name";

        /// <summary>
        /// The name of the node (which is not necessarily unique).
        /// </summary>
        public string SourceName
        {
            get => GetString(sourcenameAttribute);
            set => SetString(sourcenameAttribute, value);
        }

        [SerializeField]
        private Node parent;

        /// <summary>
        /// The ancestor of the node in the hierarchy. May be null if the node
        /// is a root.
        /// </summary>
        [SerializeField]
        public Node Parent
        {
            get => parent;
            set => parent = value;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": node,\n";
            result += base.ToString();
            result += "}";
            return result;
        }

        [SerializeField]
        private List<Node> children = new List<Node>();

        /// <summary>
        /// The number of descendants of this node.
        /// </summary>
        /// <returns>number of descendants</returns>
        public int NumberOfChildren()
        {
            return children.Count;
        }

        /// <summary>
        /// The descendants of the node. 
        /// Note: This is not a copy. Do not modify the result.
        /// </summary>
        /// <returns>descendants of the node</returns>
        public List<Node> Children()
        {
            return children;
        }

        /// <summary>
        /// Add given node as a descendant of the node in the hierarchy.
        /// The same node must not be added more than once.
        /// </summary>
        /// <param name="child">descendant to be added to node</param>
        public void AddChild(Node child)
        {
            if (child.Parent == null)
            {
                children.Add(child);
                child.Parent = this;
            }
            else
            {
                throw new System.Exception("Hierarchical edges do not form a tree. Node with multiple parents: "
                    + child.LinkName);
            }
        }
    }
}