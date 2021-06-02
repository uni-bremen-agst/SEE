using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Used to recalculate the incoming and outgoing edges of a node, as well as the edges of its children. 
    /// </summary>
    public static class GameEdgeMover
    {
        /// <summary>
        /// If <paramref name="node"/> is a root, an empty set is returned; otherwise
        /// returns the given <paramref name="node"/> and all its transitive ancestors.
        /// </summary>
        /// <param name="node">node of the node-decomposition hierarchy to be returned</param>
        /// <returns>All nodes found including the <paramref name="node"/> or empty set
        /// is <paramref name="node"/> is a root</returns>
        private static ISet<Node> GetAllChildNodes(Node node)
        {
            // If the node is the root we can ignore it, because all nodes and
            // edges are contained in it and are automatically scaled.
            if (node.IsRoot())
            {
                return new HashSet<Node>();
            }

            ISet<Node> listOfChildNodes = new HashSet<Node>
            {
                node
            };

            AddChildNode(node);
            return listOfChildNodes;

            void AddChildNode(Node node)
            {
                foreach (Node childNode in node.Children())
                {
                    listOfChildNodes.Add(childNode);
                    AddChildNode(childNode);
                }
            }
        }

        /// <summary>
        /// Moves all incoming and outgoing edges of a node and its children
        /// if they are not in the same inner node,
        /// can be used for moving inner nodes to reduce computational effort
        /// </summary>
        /// <param name="node">node whose edges are to be moved</param>
        public static void MoveAllConnectingEdgesOfNodeConnectingToAnotherSubset(GameObject node)
        {
            MoveEdgesOfNode(node, false);
        }

        /// <summary>
        /// Moves all incoming and outgoing edges of a node and its children depending on the new position of the node.
        /// </summary>
        /// <param name="node">node whose edges are to be moved</param>
        public static void MoveAllConnectingEdgesOfNode(GameObject node)
        {
            MoveEdgesOfNode(node, true);
        }

        /// <summary>
        /// Searches for all children nodes of the passed node and calculates for their incoming and outgoing edges
        /// the corresponding new positions and finally moves those edges to the calculated positions.
        /// </summary>
        /// <param name="node">The node for which and its children the incoming and outgoing edges are to be recalculated and displayed</param>
        /// <param name="allNodes">If true, then all edges for the node and its children should be moved,
        /// if false, then only those edges should be moved that have a connection between a node
        /// and another node that is located in another inner node.</param>
        private static void MoveEdgesOfNode(GameObject node, bool allNodes)
        {
            ISet<Node> nodeList = new HashSet<Node>();
            // Search for the node in question and its children.
            foreach (GameObject n in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (n.activeInHierarchy && n.ID().Equals(node.ID()))
                {
                    if (n.TryGetComponent(out NodeRef _))
                    {
                        nodeList = GetAllChildNodes(n.GetNode());
                        break;
                    }
                }
            }
            HashSet<(GameObject, GameObject, string)> backupSourceTargetEdge = new HashSet<(GameObject, GameObject, string)>();

            foreach (Node nodeToBeRedrawn in nodeList)
            {
                ISet<Edge> edgesToBeRedrawn = new HashSet<Edge>();
                // Search for all incoming and outgoing edges.
                foreach (GameObject n in GameObject.FindGameObjectsWithTag(Tags.Node))
                {
                    if (n.activeInHierarchy && n.ID().Equals(nodeToBeRedrawn.ID))
                    {
                        if (n.TryGetComponent(out NodeRef nodeRef))
                        {
                            edgesToBeRedrawn.UnionWith(nodeRef.Value.Outgoings.Union(nodeRef.Value.Incomings));
                        }
                    }
                }

                ISet<(GameObject, GameObject, string)> sourceTargetEdge = new HashSet<(GameObject, GameObject, string)>();

                // Search for the corresponding GameObjects.
                foreach (Edge edge in edgesToBeRedrawn)
                {
                    GameObject source = GameObject.Find(edge.Source.ID);
                    GameObject target = GameObject.Find(edge.Target.ID);

                    if (source != null && target != null)
                    {
                        sourceTargetEdge.Add((source, target, edge.ID));
                    }
                }
                // Sort out all duplicate edges.
                foreach ((GameObject, GameObject, string) element in sourceTargetEdge)
                {
                    if (!backupSourceTargetEdge.Any(item => item.Item3.Equals(element.Item3)))
                    {
                        backupSourceTargetEdge.Add(element);
                    }
                }
            }
            if (allNodes)
            {
                RedrawEdges(backupSourceTargetEdge);
            }
            else
            {
                ISet<(GameObject, GameObject, string)> sourceTargetEdgeWithinSameSubset = new HashSet<(GameObject, GameObject, string)>();

                // Looks if the edge represents the connection between two nodes in the same inner node, if so it can be ignored.
                foreach ((GameObject, GameObject, string) element in backupSourceTargetEdge)
                {
                    if (!nodeList.Contains(element.Item1.GetNode()) || !nodeList.Contains(element.Item2.GetNode()))
                    {
                        sourceTargetEdgeWithinSameSubset.Add(element);
                    }
                }
                RedrawEdges(sourceTargetEdgeWithinSameSubset);
            }
        }

        /// <summary>
        /// Deletes all edges in the set and then draws them at their new position.
        /// </summary>
        /// <param name="edges">The edges that are to be redrawn</param>
        private static void RedrawEdges(ISet<(GameObject, GameObject, string)> edges)
        {
            // Delete all old edges.
            foreach ((GameObject, GameObject, string) element in edges)
            {
                // FIXME GameObject.Find is an expensive operation.
                GameObject edgeToBeRedrawn = GameObject.Find(element.Item3);
                // Removes the edge from the graph.
                GameEdgeAdder.Remove(edgeToBeRedrawn);
                // Removes the edge game object.
                Object.Destroy(edgeToBeRedrawn);
            }
            // Create all new edges.
            foreach ((GameObject, GameObject, string) element in edges)
            {
                GameEdgeAdder.Add(element.Item1, element.Item2, element.Item3);
            }
        }
    }
}