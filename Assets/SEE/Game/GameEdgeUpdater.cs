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
    public static class GameEdgeUpdater
    {
        /// <summary>
        /// Searches all inner nodes for a node.
        /// </summary>
        /// <param name="node">The node for which all inner nodes are to be searched for</param>
        /// <returns>All nodes found including the <paramref name="node"/></returns>
        private static ISet<Node> GetAllChildNodes(Node node)
        {
            ISet<Node> listOfChildNodes = new HashSet<Node>
            {
                node
            };
            AddChildNode(node);
            return listOfChildNodes;

            void AddChildNode(Node node)
            {
                if (node.IsLeaf() || node.IsRoot())
                {
                    return;
                }
                foreach (Node childNode in node.Children())
                {
                    listOfChildNodes.Add(childNode);
                    AddChildNode(childNode);
                }
            }
        }

        /// <summary>
        /// Recalculates all edges of a node, as well as the edges of its children.
        /// </summary>
        /// <param name="node">The node whose edges are to be recalculated</param>
        public static void UpdateAllConnectingEdgesOfNode(GameObject node)
        {
            ISet<Node> nodeList = new HashSet<Node>();
            // Search for the node in question and its children
            foreach (GameObject n in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (n.activeInHierarchy && n.ID().Equals(node.ID()))
                {
                    if (n.TryGetComponent(out NodeRef _))
                    {
                        // FIXME: Is a break missing here?
                        nodeList = GetAllChildNodes(n.GetNode());
                        break;
                    }
                }
            }

            HashSet<(GameObject, GameObject, string)> backupSourceTargetEdge = new HashSet<(GameObject, GameObject, string)>();

            foreach (Node nodeToBeRedrawn in nodeList)
            {
                ISet<Edge> edgesToBeRedrawn = new HashSet<Edge>();
                ///Search for all incoming and outgoing edges
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

                // Search for the corresponding GameObjects
                foreach (Edge edge in edgesToBeRedrawn)
                {
                    GameObject source = GameObject.Find(edge.Source.ID);
                    GameObject target = GameObject.Find(edge.Target.ID);

                    if (source != null && target != null)
                    {
                        sourceTargetEdge.Add((source, target, edge.ID));
                    }
                }
                //Sort out all duplicate edges
                foreach ((GameObject, GameObject, string) element in sourceTargetEdge)
                {
                    if (!backupSourceTargetEdge.Any(item => item.Item3.Equals(element.Item3)))
                    {
                        backupSourceTargetEdge.Add(element);
                    }
                }
            }
            //Delete all old edges
            foreach ((GameObject, GameObject, string) element in backupSourceTargetEdge)
            {
                GameEdgeAdder.Remove(GameObject.Find(element.Item3));
            }
            //Create all new edges
            foreach ((GameObject, GameObject, string) element in backupSourceTargetEdge)
            {
                GameEdgeAdder.Add(element.Item1, element.Item2, element.Item3);
            }
        }
    }
}
