﻿using System;
using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Node of a graph.
    /// </summary>
    public class Node : GraphElement
    {
        // IMPORTANT NOTES:
        //
        // If you use Clone() to create a copy of a node, be aware that the clone
        // will have a deep copy of all attributes and the type of the node only.
        // The hierarchy information (parent, children, level) is not copied at all.
        // The clone will appear as a node without parent and children at level 0.
        // Neither will its incoming and outgoing edges be copied.

        /// <summary>
        /// The attribute name for unique identifiers (within a graph).
        /// </summary>
        public const string LinknameAttribute = "Linkage.Name";

        /// <summary>
        /// The unique identifier of a node (unique within a graph).
        /// </summary>
        private string id = "";

        /// <summary>
        /// The unique identifier of a node (unique within a graph).
        /// 
        /// Important note on setting this property:
        /// This will only set the id attribute, but does not alter the
        /// hashed ids of the underlying graph. If the node was already
        /// added to a graph, you cannot change the ID anymore. 
        /// Otherwise expect inconsistencies. If the node has not been added
        /// to a graph, however, setting this property is safe.
        /// </summary>
        public override string ID
        {
            get => id;
            set => id = value;
        }

        /// <summary>
        /// The attribute name for the name of nodes. They may or may not be unique.
        /// </summary>
        public const string SourceNameAttribute = "Source.Name";

        /// <summary>
        /// The name of the node (which is not necessarily unique).
        /// </summary>
        public string SourceName
        {
            get => GetString(SourceNameAttribute);
            set => SetString(SourceNameAttribute, value);
        }

        /// <summary>
        /// The parent of this node. Is null if it has none.
        /// </summary>
        private Node parent;

        /// <summary>
        /// The level of the node in the hierarchy. The top-most level has level 
        /// number 0. The number is the length of the path in the hierarchy from
        /// the node to its ancestor that has no parent.
        /// </summary>
        private int level = 0;

        /// <summary>
        /// The level of a node in the hierarchy. The level of a root node is 0.
        /// For all other nodes, the level is the level of its parent + 1.
        /// </summary>
        public int Level
        {
            get => level;
        }

        /// <summary>
        /// Sets the level of the node as specified by the parameter and sets
        /// the respective level values of each of its (transitive) descendants. 
        /// </summary>
        internal void SetLevel(int level)
        {
            this.level = level;
            foreach (Node child in children)
            {
                child.SetLevel(level + 1);
            }
        }

        /// <summary>
        /// Returns the maximal depth of the tree rooted by this node, that is,
        /// the number of nodes on the longest path from this node to any of its 
        /// leaves. The minimal value returned is 1.
        /// </summary>
        /// <returns>maximal depth of the tree rooted by this node</returns>
        public int Depth()
        {
            int maxDepth = 0;

            foreach (Node child in children)
            {
                int depth = child.Depth();
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }
            return maxDepth + 1;
        }

        /// <summary>
        /// The ancestor of the node in the hierarchy. May be null if the node is a root.
        /// </summary>
        public Node Parent
        {
            get => parent;
            set => parent = value;
        }

        /// <summary>
        /// True iff node has no parent.
        /// </summary>
        /// <returns>true iff node is a root node</returns>
        public bool IsRoot()
        {
            return parent == null;
        }

        /// <summary>
        /// Yields the set of all transitive parents of this node in the node hierarchy
        /// including the node itself.
        /// </summary>
        /// <returns>ascendants of this node in the hierarchy including the node itself</returns>
        public List<Node> Ascendants()
        {
            List<Node> result = new List<Node>();
            Node cursor = this;
            while (cursor != null)
            {
                result.Add(cursor);
                cursor = cursor.Parent;
            }
            return result;
        }

        /// <summary>
        /// Returns true if <paramref name="other"/> if other meets all of the following 
        /// conditions:
        ///  (1) is not null
        ///  (2) has exactly the same C# type
        ///  (3) has the same type name
        ///  (4) has exactly the same attributes with exactly the same values
        ///  (5) has a parent with the same ID as the parent of this node
        ///  (6) has the same level
        ///  (7) has the same number of children
        ///  (8) the set of IDs of the children are the same
        ///  (9) has the same number of outgoing edges and the set of the edges' IDs are the same
        /// (10) has the same number of incoming edges and the set of the edges' IDs are the same
        ///  
        /// Note: This node and the other node may or may not be in the same graph.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(System.Object other)
        {
            if (!base.Equals(other))
            {
                return false;
            }
            else
            {
                Node otherNode = other as Node;
                if (level != otherNode.level)
                {
                    Report(ID + ": The levels are different");
                    return false;
                }
                else if ((Parent == null && otherNode.Parent != null)
                          || ((Parent != null && otherNode.Parent == null)))
                {
                    Report(ID + ": The parents are different (only one of them is null)");
                    return false;
                }
                else if (Parent != null && otherNode.Parent != null
                          && Parent.ID != otherNode.Parent.ID)
                {
                    Report(ID + ": The parents' IDs are different");
                    return false;
                }
                else if (NumberOfChildren() != otherNode.NumberOfChildren()
                         || !GetIDs(children).SetEquals(GetIDs(otherNode.children)))
                {
                    Report(ID + ": The children are different.");
                    return false;
                }
                else if (outgoings.Count != otherNode.outgoings.Count
                         || !GetIDs(outgoings).SetEquals(GetIDs(otherNode.outgoings)))
                {
                    Report(ID + ": The outgoing edges are different.");
                    return false;
                }
                else if (incomings.Count != otherNode.incomings.Count
                         || !GetIDs(incomings).SetEquals(GetIDs(otherNode.incomings)))
                {
                    Report(ID + ": The incoming edges are different.");
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns the set of IDs of all given <paramref name="graphElements"/>.
        /// </summary>
        /// <typeparam name="T">a GraphElement type</typeparam>
        /// <param name="graphElements">the graph elements whose IDs are to be collected</param>
        /// <returns>IDs of all given <paramref name="graphElements"/></returns>
        private HashSet<string> GetIDs<T>(IList<T> graphElements) where T : GraphElement
        {
            HashSet<string> result = new HashSet<string>();
            foreach (GraphElement graphElement in graphElements)
            {
                result.Add(graphElement.ID);
            }
            return result;
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            // we are using the ID which is intended to be unique
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": node,\n";
            result += base.ToString();
            result += "}";
            return result;
        }

        /// <summary>
        /// The incoming edges of this node.
        /// </summary>
        private List<Edge> incomings = new List<Edge>();

        /// <summary>
        /// The incoming edges of this node.
        /// </summary>
        public List<Edge> Incomings
        {
            get => incomings;
        }

        /// <summary>
        /// Adds given edge to the list of incoming edges of this node.
        /// 
        /// IMPORTANT NOTE: This method is intended for Graph only. Other clients 
        /// should use Graph.AddEdge() instead.
        /// 
        /// Precondition: edge != null and edge.Target == this
        /// </summary>
        /// <param name="edge">edge to be added as one of the node's incoming edges</param>
        public void AddIncoming(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new Exception("edge must not be null");
            }
            else if (edge.Target != this)
            {
                throw new Exception("edge " + edge.ToString() + " is no incoming edge of " + ToString());
            }
            else
            {
                incomings.Add(edge);
            }
        }

        /// <summary>
        /// Removes given edge from the list of incoming edges of this node.
        /// 
        /// IMPORTANT NOTE: This method is intended for Graph only. Other clients 
        /// should use Graph.RemoveEdge() instead.
        /// 
        /// Precondition: edge != null and edge.Target == this
        /// </summary>
        /// <param name="edge">edge to be removed from the node's incoming edges</param>
        public void RemoveIncoming(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new Exception("edge must not be null");
            }
            else if (edge.Target != this)
            {
                throw new Exception("edge " + edge.ToString() + " is no incoming edge of " + ToString());
            }
            else
            {
                if (!incomings.Remove(edge))
                {
                    throw new Exception("edge " + edge.ToString() + " is no incoming edge of " + ToString());
                }
            }
        }

        /// <summary>
        /// The outgoing edges of this node.
        /// </summary>
        private List<Edge> outgoings = new List<Edge>();

        /// <summary>
        /// The outgoing edges of this node.
        /// </summary>
        public List<Edge> Outgoings
        {
            get => outgoings;
        }

        /// <summary>
        /// Removes all incoming and outgoing edges from this node.
        /// 
        /// IMPORTANT NOTE: This method is reserved for Graph and should not
        /// be used by any other client.
        /// </summary>
        public void RemoveAllEdges()
        {
            outgoings.Clear();
            incomings.Clear();
        }

        /// <summary>
        /// Adds given edge to the list of outgoing edges of this node.
        /// 
        /// IMPORTANT NOTE: This method is intended for Graph only. Other clients 
        /// should use Graph.AddEdge() instead.
        /// 
        /// Precondition: edge != null and edge.Source == this
        /// </summary>
        /// <param name="edge">edge to be added as one of the node's outgoing edges</param>
        public void AddOutgoing(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new Exception("edge must not be null");
            }
            else if (edge.Source != this)
            {
                throw new Exception("edge " + edge.ToString() + " is no outgoing edge of " + ToString());
            }
            else
            {
                outgoings.Add(edge);
            }
        }

        /// <summary>
        /// Removes given edge from the list of outgoing edges of this node.
        /// 
        /// IMPORTANT NOTE: This method is intended for Graph only. Other clients 
        /// should use Graph.RemoveEdge() instead.
        /// 
        /// Precondition: edge != null and edge.Source == this
        /// </summary>
        /// <param name="edge">edge to be removed from the node's outgoing edges</param>
        public void RemoveOutgoing(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new Exception("edge must not be null");
            }
            else if (edge.Source != this)
            {
                throw new Exception("edge " + edge.ToString() + " is no outgoing edge of " + ToString());
            }
            else
            {
                if (!outgoings.Remove(edge))
                {
                    throw new Exception("edge " + edge.ToString() + " is no outgoing edge of " + ToString());
                }
            }
        }

        /// <summary>
        /// The list of immediate children of this node in the hierarchy.
        /// </summary>
        private List<Node> children = new List<Node>();

        /// <summary>
        /// The number of immediate children of this node in the hierarchy.
        /// </summary>
        /// <returns>number of immediate children</returns>
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
            if (ReferenceEquals(child.Parent, null))
            {
                children.Add(child);
                child.Parent = this;
            }
            else
            {
                throw new Exception("Hierarchical edges do not form a tree. Node with multiple parents: "
                    + child.ID);
            }
        }

        /// <summary>
        /// Re-assigns the node to a different <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent">the new parent of this node</param>
        public void Reparent(Node newParent)
        {
            if (this == newParent)
            {
                throw new Exception("Circular dependency. A node cannot become its own parent.");
            }
            else if (newParent == null)
            {
                // Nothing do be done for newParent == null and parent == null.
                if (parent != null)
                {
                    parent.children.Remove(this);
                    parent = null;
                    graph.FinalizeNodeHierarchy();
                }
            }
            else
            {
                // assert: newParent != null
                if (parent == null)
                {
                    newParent.AddChild(this);                    
                }
                else
                {
                    // parent != null and newParent != null
                    parent.children.Remove(this);
                    parent = newParent;
                    parent.children.Add(this);
                }
                graph.FinalizeNodeHierarchy();
            }
        }

        /// <summary>
        /// Sorts the list of children using the given comparison.
        /// </summary>
        /// <param name="comparison"></param>
        public void SortChildren(Comparison<Node> comparison)
        {
            List<Node> sortedChildren = children;
            sortedChildren.Sort(comparison);
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
        /// Where name(n) denotes the Source.Name of n if it has one or otherwise its ID.
        /// </summary>
        /// <param name="first">first node to be compared</param>
        /// <param name="second">second node to be compared</param>
        /// <returns>0 if equal, -1 if first < second, 1 if first > second</returns>
        public static int CompareTo(Node first, Node second)
        {
            if (ReferenceEquals(first, null))
            {
                if (ReferenceEquals(second, null))
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
                if (ReferenceEquals(second, null))
                // ...and second is null, first is greater.
                {
                    return 1;
                }
                else
                {
                    string firstName = first.SourceName;
                    if (string.IsNullOrEmpty(firstName))
                    {
                        firstName = first.ID;
                    }
                    string secondName = second.SourceName;
                    if (string.IsNullOrEmpty(secondName))
                    {
                        secondName = second.ID;
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

        /// <summary>
        /// True if node is an inner node, i.e., has children.
        /// </summary>
        /// <returns>true iff inner node</returns>
        public bool IsInnerNode()
        {
            return children.Count > 0;
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// 
        /// IMPORTANT NOTE: Cloning a node means only to create copy of its
        /// type and attributes. The hierarchy information (parent, level,
        /// and children) are not copied. Instead the copy will become a 
        /// node without parent and children at level 0. If we copied the
        /// hierarchy information, the hierarchy were no longer a forrest.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            Node target = (Node)clone;
            target.parent = null;
            target.level = 0;
            target.children = new List<Node>();
            target.outgoings = new List<Edge>();
            target.incomings = new List<Edge>();
        }

        /// <summary>
        /// Returns the list of outgoing edges from this node to the given 
        /// target node that have exactly the given edge type.
        /// 
        /// Precondition: target must not be null
        /// </summary>
        /// <param name="target">target node</param>
        /// <param name="its_type">requested edge type</param>
        /// <returns>all edges from this node to target node with exactly the given edge type</returns>
        public List<Edge> From_To(Node target, string its_type)
        {
            if (ReferenceEquals(target, null))
            {
                throw new Exception("target node must not be null");
            }
            else
            {
                List<Edge> result = new List<Edge>();

                foreach (Edge edge in outgoings)
                {
                    if (edge.Target == target && edge.Type == its_type)
                    {
                        result.Add(edge);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Yields true if there is an edge, E, from this node to <paramref name="other"/> node.
        /// If <paramref name="edgeType"/> is null or the empty string, E may have any edge type.
        /// Otherwise, E.Type = <paramref name="edgeType"/> must hold.
        /// </summary>
        /// <param name="other">other node to be checked for successorship</param>
        /// <param name="edgeType">the requested edge type; may be null or empty</param>
        /// <returns>true if <paramref name="other"/>is a successor</returns>
        public bool HasSuccessor(Node other, string edgeType)
        {
            foreach (Edge outgoing in Outgoings)
            {
                if (outgoing.Target == other
                    && (string.IsNullOrEmpty(edgeType) || outgoing.Type == edgeType))
                {
                    return true;
                }
            }
            return false;
        }
    }
}