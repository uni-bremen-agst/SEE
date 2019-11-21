using System;
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

        public const string LinknameAttribute = "Linkage.Name";

        /// <summary>
        /// The unique identifier of a node.
        /// </summary>
        [SerializeField]
        public string LinkName
        {
            get => GetString(LinknameAttribute);
            // This will only set the linkname attribute, but does not alter the
            // hashed linknames of the underlying graph. You will likely want to
            // use Graph.SetLinkname instead. Otherwise expect inconsistencies.
            // This setter should only be called by Graph.SetLinkname.
            set => SetString(LinknameAttribute, value);
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

        /// <summary>
        /// Sorts the list of children using the given comparison.
        /// </summary>
        /// <param name="comparison"></param>
        public void SortChildren(Comparison<Node> comparison)
        {
            children.Sort(comparison);
        }

        /// <summary>
        /// Compares the two given nodes by name. 
        /// 
        /// Returns 0 if:
        ///    both are null
        ///    or name(first) = name(second)
        /// Returns -1 if:
        ///    first is null and second is not null
        ///    or name(first) < name(second)
        /// Returns 1 if:
        ///    second is null and first is not null
        ///    or name(first) > name(second)
        /// Where name(n) denotes the Source.Name of n if it has one or otherwise its Linkage.Name.
        /// </summary>
        /// <param name="first">first node to be compared</param>
        /// <param name="second">second node to be compared</param>
        /// <returns></returns>
        public static int CompareTo(Node first, Node second)
        {
            if (first == null)
            {
                if (second == null)
                {
                    // If first is null and second is null, they're equal. 
                    return 0;
                }
                else
                {
                    // If first is null and second is not null, second is greater. 
                    return -1;
                }
            }
            else
            {
                // If first is not null...
                if (second == null)
                // ...and second is null, first is greater.
                {
                    return 1;
                }
                else
                {
                    string firstName = first.SourceName;
                    if (string.IsNullOrEmpty(firstName))
                    {
                        firstName = first.LinkName;
                    }
                    string secondName = second.SourceName;
                    if (string.IsNullOrEmpty(secondName))
                    {
                        secondName = second.LinkName;
                    }
                    return firstName.CompareTo(secondName);
                }
            }
        }

        /// <summary>
        /// True if node is a leaf, i.e., has no children.
        /// </summary>
        /// <returns>true iff leaf node</returns>
        public bool IsLeaf()
        {
            return children.Count == 0;
        }
    }
}