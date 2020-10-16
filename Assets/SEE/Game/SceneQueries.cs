using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides queries on game objects in the current scence at run-time.
    /// </summary>
    internal class SceneQueries
    {
        /// <summary>
        /// Returns all game objects in the current scene tagged by Tags.Node and having 
        /// a valid reference to a graph node.
        /// </summary>
        /// <returns>all game objects representing graph nodes in the scene</returns>
        public static ICollection<GameObject> AllGameNodesInScene(bool includeLeaves, bool includeInnerNodes)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (go.TryGetComponent<NodeRef>(out NodeRef nodeRef))
                {
                    Node node = nodeRef.node;
                    if (node != null)
                    {
                        if ((includeLeaves && node.IsLeaf()) || (includeInnerNodes && !node.IsLeaf()))
                        {
                            result.Add(go);
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("Game node {0} has a null node reference.\n", go.name);
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Game node {0} without node reference.\n", go.name);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the roots of all graphs currently represented by any of the <paramref name="gameNodes"/>.
        /// 
        /// Precondition: Every game object in <paramref name="gameNodes"/> must be tagged by
        /// Tags.Node and have a valid graph node reference.
        /// </summary>
        /// <param name="gameNodes">game nodes whose roots are to be returned</param>
        /// <returns>all root nodes in the scene</returns>
        public static ICollection<Node> GetRoots(ICollection<GameObject> gameNodes)
        {
            List<Node> result = new List<Node>();
            foreach (Graph graph in GetGraphs(gameNodes))
            {
                result.AddRange(graph.GetRoots());
            }
            return result;
        }

        /// <summary>
        /// Returns all graphs currently represented by any of the <paramref name="gameNodes"/>.
        /// 
        /// Precondition: Every game object in <paramref name="gameNodes"/> must be tagged by
        /// Tags.Node and have a valid graph node reference.
        /// </summary>
        /// <param name="gameNodes">game nodes whose graph is to be returned</param>
        /// <returns>all graphs in the scene</returns>
        public static HashSet<Graph> GetGraphs(ICollection<GameObject> gameNodes)
        {
            HashSet<Graph> result = new HashSet<Graph>();
            foreach (GameObject go in gameNodes)
            {
                result.Add(go.GetComponent<NodeRef>().node.ItsGraph);
            }
            return result;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents a leaf in the graph.
        /// 
        /// Precondition: <paramref name="gameNode"/> has a NodeRef component attached to it
        /// that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>true if <paramref name="gameNode"/> represents a leaf in the graph</returns>
        public static bool IsLeaf(GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>().node.IsLeaf();
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents an inner node in the graph.
        /// 
        /// Precondition: <paramref name="gameNode"/> has a NodeRef component attached to it
        /// that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>true if <paramref name="gameNode"/> represents an inner node in the graph</returns>
        public static bool IsInnerNode(GameObject gameNode)
        {
            return !gameNode.GetComponent<NodeRef>().node.IsLeaf();
        }
    }
}