﻿using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

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
        // will have a deep copy of all attributes and the type and domain of the node only.
        // The hierarchy information (parent, children, level) is not copied at all.
        // The clone will appear as a node without parent and children at level 0.
        // Neither will its incoming and outgoing edges be copied.

        /// <summary>
        /// The attribute name for unique identifiers (within a graph).
        /// </summary>
        public const string LinknameAttribute = "Linkage.Name";

        private string id = "";
        /// <summary>
        /// The unique identifier of a node (unique within a graph).
        /// Setting a new id will also set set a new <see cref="LinknameAttribute"/>.
        ///
        /// Important note on setting this property:
        /// This will only set the id attribute, but does not alter the
        /// hashed ids of the underlying graph. If the node was already
        /// added to a graph, you cannot change the ID anymore.
        /// If the node has not been added to a graph, however, setting this property is safe.
        /// </summary>
        public override string ID
        {
            get => id;
            set
            {
                if (ItsGraph != null)
                {
                    throw new InvalidOperationException("ID must not be changed once added to graph.");
                }
                id = value;
                SetString(LinknameAttribute, id);
            }
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
            get => TryGetString(SourceNameAttribute, out string sourceName) ? sourceName : null;
            set => SetString(SourceNameAttribute, value);
        }

        /// <summary>
        /// The level of the node in the hierarchy. The top-most level has level
        /// number 0. The number is the length of the path in the hierarchy from
        /// the node to its ancestor that has no parent.
        /// </summary>
        private int level = 0;

        /// <summary>
        /// The level of a node in the hierarchy. The level of a root node is 0.
        /// For all other nodes, the level is the level of its parent + 1.
        /// The level of a node that is currently in no graph is 0.
        /// </summary>
        public int Level
        {
            get
            {
                if (ItsGraph == null)
                {
                    return 0;
                }
                if (ItsGraph.NodeHierarchyHasChanged)
                {
                    ItsGraph.FinalizeNodeHierarchy();
                }
                return level;
            }
        }

        /// <summary>
        /// Sets the level of the node as specified by the parameter and sets
        /// the respective level values of each of its (transitive) descendants.
        ///
        /// Note: This method should be called only by <see cref="Graph"/>.
        /// </summary>
        internal void SetLevel(int newLevel)
        {
            level = newLevel;
            foreach (Node child in children)
            {
                child.SetLevel(newLevel + 1);
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
            int maxDepth = children.Select(child => child.Depth()).Prepend(0).Max();
            return maxDepth + 1;
        }

        /// <summary>
        /// The ancestor of the node in the hierarchy. May be null if the node is a root.
        /// </summary>
        private Node parent;

        /// <summary>
        /// The ancestor of the node in the hierarchy. May be null if the node is a root.
        /// </summary>
        public Node Parent {
            get => parent;
            private set
            {
                Node oldParent = parent;
                parent = value;
                switch (value)
                {
                    case null when oldParent == null: // Nothing to be done.
                        break;
                    case null: // value is null while parent is not, so the parent has been removed.
                        Notify(new HierarchyEvent(Version, oldParent, this, ChangeType.Removal));
                        break;
                    default: // value != null, so the parent has been added or changed
                        Notify(new HierarchyEvent(Version, value, this, ChangeType.Addition));
                        break;
                }
            }
        }

        /// <summary>
        /// True iff node has no parent.
        /// </summary>
        /// <returns>true iff node is a root node</returns>
        public bool IsRoot()
        {
            return Parent == null;
        }

        /// <summary>
        /// True iff node has the <see cref="Graph.RootToggle"/> marking.
        /// </summary>
        /// <returns>true iff node has the <see cref="Graph.RootToggle"/> marking.</returns>
        public bool HasRootToogle()
        {
            return HasToggle(Graph.RootToggle);
        }

        /// <summary>
        /// True iff node is the implementation or the architecture root of a reflexion city.
        /// </summary>
        /// <returns>true iff node has the <see cref="Graph.RootToggle"/> and the implementation or architecture marking.</returns>
        public bool IsArchitectureOrImplementationRoot()
        {
            return HasToggle(Graph.RootToggle) &&
                (HasToggle(ReflexionSubgraphs.Implementation.GetLabel()) || HasToggle(ReflexionSubgraphs.Architecture.GetLabel()));
        }

        /// <summary>
        /// True iff node is the architecture root of a reflexion city.
        /// </summary>
        /// <returns>true iff node has the <see cref="Graph.RootToggle"/> and the architecture marking.</returns>
        public bool IsArchitectureRoot()
        {
            return HasRootToogle() && HasToggle(ReflexionSubgraphs.Architecture.GetLabel());
        }

        /// <summary>
        /// True iff node is implementation root of a reflexion city.
        /// </summary>
        /// <returns>true iff node has <see cref="Graph.RootToggle"/> and the implementation marking.</returns>
        public bool IsImplementationRoot()
        {
            return HasRootToogle() && HasToggle(ReflexionSubgraphs.Implementation.GetLabel());
        }

        /// <summary>
        /// Yields the set of all transitive parents of this node in the node hierarchy
        /// including the node itself.
        /// </summary>
        /// <returns>ascendants of this node in the hierarchy including the node itself</returns>
        public IList<Node> Ascendants()
        {
            List<Node> result = new();
            Node cursor = this;
            while (cursor != null)
            {
                result.Add(cursor);
                cursor = cursor.Parent;
            }
            return result;
        }

        /// <summary>
        /// Returns all transitive descendants of this node in a post-order traversal of the
        /// node hierarchy rooted by this node, including this node itself (will be the last node
        /// in the returned ordered list).
        /// </summary>
        /// <returns>transitive descendants of this node in post order</returns>
        public IList<Node> PostOrderDescendants()
        {
            // FIXME: Check for cycles in the graph, aborting with an appropriate exception (maybe in a separate method)
            IList<Node> result = new List<Node>();
            PostOrderDescendants(this);
            return result;

            void PostOrderDescendants(Node parent)
            {
                foreach (Node child in parent.Children())
                {
                    PostOrderDescendants(child);
                }
                result.Add(parent);
            }
        }

        /// <summary>
        /// Returns all transitive descendants of this node in a post-order traversal of the
        /// node hierarchy rooted by this node, excluding this node itself.
        /// </summary>
        /// <returns>transitive descendants of this node in post order without itself.</returns>
        public IList<Node> PostOrderDescendantsWithoutItself()
        {
            IList<Node> result = PostOrderDescendants();
            result.RemoveAt(result.Count - 1);
            return result;
        }

        /// <summary>
        /// Returns the set of IDs of all given <paramref name="graphElements"/>.
        /// </summary>
        /// <typeparam name="T">a GraphElement type</typeparam>
        /// <param name="graphElements">the graph elements whose IDs are to be collected</param>
        /// <returns>IDs of all given <paramref name="graphElements"/></returns>
        private static HashSet<string> GetIDs<T>(IEnumerable<T> graphElements) where T : GraphElement
        {
            HashSet<string> result = new();
            foreach (T graphElement in graphElements)
            {
                result.Add(graphElement.ID);
            }
            return result;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": node,\n";
            result += base.ToString();
            result += "}";
            return result;
        }

        public override string ToShortString()
        {
            return $"{SourceName} [{Type}]";
        }

        /// <summary>
        /// The incoming edges of this node.
        /// </summary>
        public ISet<Edge> Incomings { get; private set; } = new HashSet<Edge>();

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
                throw new Exception($"edge {edge} is no incoming edge of {ToString()}");
            }
            else
            {
                Incomings.Add(edge);
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
                throw new Exception($"edge {edge} is no incoming edge of {ToString()}");
            }
            else
            {
                if (!Incomings.Remove(edge))
                {
                    throw new Exception($"edge {edge} is no incoming edge of {ToString()}");
                }
            }
        }

        /// <summary>
        /// The outgoing edges of this node.
        /// </summary>
        public ISet<Edge> Outgoings { get; private set; } = new HashSet<Edge>();

        /// <summary>
        /// Returns all outgoing edges of given node that have exactly the given <paramref name="edgeType"/>.
        /// If <paramref name="edgeType"/> is <c>null</c> or empty, all outgoing edges are returned.
        /// </summary>
        /// <param name="edgeType">the requested exact edge type (may be null or empty)</param>
        /// <returns>all outgoing edges of <paramref name="edgeType"/></returns>
        public IEnumerable<Edge> OutgoingsOfType(string edgeType)
        {
            return Outgoings.Where(edge => string.IsNullOrEmpty(edgeType) || edge.Type == edgeType);
        }

        /// <summary>
        /// Returns all incoming edges of given node that have exactly the given <paramref name="edgeType"/>.
        /// If <paramref name="edgeType"/> is <c>null</c> or empty, all incoming edges are returned.
        /// </summary>
        /// <param name="edgeType">the requested exact edge type (may be null or empty)</param>
        /// <returns>all incoming edges of <paramref name="edgeType"/></returns>
        public IEnumerable<Edge> IncomingsOfType(string edgeType)
        {
            return Incomings.Where(edge => string.IsNullOrEmpty(edgeType) || edge.Type == edgeType);
        }

        /// <summary>
        /// All edges connected to this node, i.e., the union of its incoming and outgoing edges.
        /// </summary>
        public ISet<Edge> Edges => Incomings.Union(Outgoings).ToHashSet();

        /// <summary>
        /// Resets this node, i.e., removes all incoming and outgoing edges
        /// and children from this node. Resets its graph and parent to null.
        ///
        /// IMPORTANT NOTE: This method is reserved for Graph and should not
        /// be used by any other client.
        /// </summary>
        public void Reset()
        {
            Outgoings.Clear();
            Incomings.Clear();
            children.Clear();
            Reparent(null);
            ItsGraph = null;
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
                throw new Exception($"edge {edge} is no outgoing edge of {ToString()}");
            }
            else
            {
                Outgoings.Add(edge);
            }
        }

        /// <summary>
        /// Returns true if this node is a descendant of <paramref name="node"/> in the
        /// node hierarchy.
        /// </summary>
        /// <param name="node">a potential ascendant of this node</param>
        /// <returns>true if this node is a descendant of <paramref name="node"/></returns>
        /// <remarks>For clarity: A node is considered its own descendant, i.e.,
        /// <c>n.IsDescendantOf(n)</c> is always <c>true</c>.</remarks>
        internal bool IsDescendantOf(Node node)
        {
            if (node == null)
            {
                throw new ArgumentException("Node must not be null");
            }
            if (ItsGraph != node.ItsGraph)
            {
                throw new ArgumentException("Nodes are in different graphs.");
            }
            Node cursor = this;
            while (cursor != null)
            {
                if (cursor == node)
                {
                    return true;
                }
                cursor = cursor.parent;
            }
            return false;
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
                throw new Exception($"edge {edge} is no outgoing edge of {ToString()}");
            }
            else
            {
                if (!Outgoings.Remove(edge))
                {
                    throw new Exception($"edge {edge} is no outgoing edge of {ToString()}");
                }
            }
        }

        /// <summary>
        /// The list of immediate children of this node in the hierarchy.
        /// </summary>
        private List<Node> children = new();

        /// <summary>
        /// The number of immediate children of this node in the hierarchy.
        /// </summary>
        /// <returns>number of immediate children</returns>
        public int NumberOfChildren()
        {
            return children.Count;
        }

        /// <summary>
        /// The immediate descendants of the node.
        /// Note: This is not a copy. The result can't be modified.
        /// </summary>
        /// <returns>immediate descendants of the node</returns>
        public IList<Node> Children()
        {
            return children.AsReadOnly();
        }

        /// <summary>
        /// Add given node as a descendant of the node in the hierarchy.
        /// The same node must not be added more than once.
        /// </summary>
        /// <param name="child">descendant to be added to node</param>
        /// <remarks>It is safe to call this method with a <paramref name="child"/>
        /// that is not yet in the graph of this node. Yet, the <paramref name="child"/>
        /// should be added right after calling this method or otherwise other
        /// methods will fail. Do not do anything with <paramref name="child"/> if you have not
        /// added it via <see cref="Graph.AddNode(Node)"/>.</remarks>
        public void AddChild(Node child)
        {
            if (child.Parent == null)
            {
                children.Add(child);
                child.Parent = this;
                child.level = level + 1;
                ItsGraph.NodeHierarchyHasChanged = true;
            }
            else
            {
                throw new Exception($"Node hierarchy does not form a tree. Node with multiple parents: {child.ID}.");
            }
        }

        /// <summary>
        /// Re-assigns the node to a different <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent">the new parent of this node</param>
        /// <remarks><paramref name="newParent"/> may be <c>null</c> in which case the given node becomes a root</remarks>
        public void Reparent(Node newParent)
        {
            if (this == newParent)
            {
                throw new Exception("Circular dependency. A node cannot become its own parent.");
            }
            else if (newParent == null)
            {
                // Nothing do be done for newParent == null and parent == null.
                if (Parent != null)
                {
                    Parent.children.Remove(this);
                    Parent = null;
                    ItsGraph.NodeHierarchyHasChanged = true;
                }
            }
            else
            {
                // assert: newParent != null
                if (Parent == null)
                {
                    newParent.AddChild(this);
                }
                else
                {
                    // parent != null and newParent != null
                    Parent.children.Remove(this);
                    Parent = newParent;
                    Parent.children.Add(this);
                }
                ItsGraph.NodeHierarchyHasChanged = true;
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
                    return string.Compare(firstName, secondName, StringComparison.Ordinal);
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
        /// hierarchy information, the hierarchy were no longer a forest.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            Node target = (Node)clone;
            target.Parent = null;
            target.level = 0;
            target.children = new List<Node>();
            target.Outgoings = new HashSet<Edge>();
            target.Incomings = new HashSet<Edge>();
        }

        /// <summary>
        /// Returns the list of outgoing edges from this node to the given
        /// target node that have exactly the given edge type.
        /// If the given edge type is null, it will not be taken into account.
        ///
        /// Precondition: target must not be null
        /// </summary>
        /// <param name="target">target node</param>
        /// <param name="itsType">requested edge type</param>
        /// <returns>all edges from this node to target node with exactly the given edge type</returns>
        public List<Edge> FromTo(Node target, string itsType = null)
        {
            if (ReferenceEquals(target, null))
            {
                throw new Exception("target node must not be null");
            }
            else
            {
                return Outgoings.Where(edge => edge.Target == target && (itsType == null || edge.Type == itsType)).ToList();
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
            return Outgoings.Any(outgoing => outgoing.Target == other && (string.IsNullOrEmpty(edgeType) || outgoing.Type == edgeType));
        }

        /// <summary>
        /// Returns true if <paramref name="node"/> is not null.
        /// </summary>
        /// <param name="node">node to be compared</param>
        public static implicit operator bool(Node node)
        {
            return node != null;
        }

        /// <summary>
        /// Removes this node and all its descendants in the node hierarchy
        /// including their incoming and outgoing edges from the graph.
        /// All deleted nodes and edges are returned in the result.
        /// </summary>
        /// <returns>all deleted nodes and edges including this node</returns>
        public SubgraphMemento DeleteTree()
        {
            SubgraphMemento result = new(ItsGraph);
            foreach (Node node in PostOrderDescendants())
            {
                result.Parents[node] = node.Parent;
                foreach (Edge edge in node.Outgoings)
                {
                    result.Edges.Add(edge);
                }
                foreach (Edge edge in node.Incomings)
                {
                    result.Edges.Add(edge);
                }
                // Removing a node will also remove all its incoming and outgoing edges.
                ItsGraph.RemoveNode(node);
            }
            return result;
        }
    }
}
