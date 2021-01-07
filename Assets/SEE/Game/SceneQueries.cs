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
                if (go.TryGetComponent(out NodeRef nodeRef))
                {
                    Node node = nodeRef.Value;
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
        /// Returns all node refs in the current scene of objects tagged by Tags.Node and
        /// having a valid reference to a graph node.
        /// </summary>
        /// <returns>all game objects representing graph nodes in the scene</returns>
        public static List<NodeRef> AllNodeRefsInScene(bool includeLeaves, bool includeInnerNodes)
        {
            List<NodeRef> result = new List<NodeRef>();
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (go.TryGetComponent(out NodeRef nodeRef))
                {
                    Node node = nodeRef.Value;
                    if (node != null)
                    {
                        if ((includeLeaves && node.IsLeaf()) || (includeInnerNodes && !node.IsLeaf()))
                        {
                            result.Add(nodeRef);
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
        public static List<Node> GetRoots(ICollection<GameObject> gameNodes)
        {
            List<Node> result = new List<Node>();
            foreach (Graph graph in GetGraphs(gameNodes))
            {
                result.AddRange(graph.GetRoots());
            }
            return result;
        }

        public static HashSet<Node> GetRoots(ICollection<NodeRef> nodeRefs)
        {
            HashSet<Node> result = new HashSet<Node>();
            foreach (NodeRef nodeRef in nodeRefs)
            {
                IEnumerable<Node> nodes = nodeRef?.Value?.ItsGraph?.GetRoots();
                if (nodes != null)
                {
                    foreach (Node node in nodes)
                    {
                        if (node != null)
                        {
                            result.Add(node);
                        }
                    }
                }
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
                result.Add(go.GetComponent<NodeRef>().Value.ItsGraph);
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
            return gameNode.GetComponent<NodeRef>()?.Value?.IsLeaf() ?? false;
        }

        public static bool IsLeaf(NodeRef nodeRef)
        {
            return nodeRef?.Value?.IsLeaf() ?? false;
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
            return gameNode.GetComponent<NodeRef>()?.Value?.IsInnerNode() ?? false;
        }

        /// <summary>
        /// Returns the Source.Name attribute of <paramref name="gameNode"/>. 
        /// If <paramref name="gameNode"/> has no valid node reference, the name
        /// of <paramref name="gameNode"/> is returned instead.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>source name of <paramref name="gameNode"/></returns>
        public static string SourceName(GameObject gameNode)
        {
            if (gameNode.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                if (nodeRef.Value != null)
                {
                    return nodeRef.Value.SourceName;
                }
            }
            return gameNode.name;
        }

        public static string SourceName(NodeRef nodeRef)
        {
            string result = string.Empty;

            if (nodeRef)
            {
                result = nodeRef.Value?.SourceName ?? nodeRef.gameObject.name;
            }

            return result;
        }

        /// <summary>
        /// Returns first child of <paramref name="codeCity"/> tagged by Tags.Node. 
        /// If <paramref name="codeCity"/> is a node representing a code city,
        /// the result is considered the root of the graph.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>game object representing the root of the graph or null if there is none</returns>
        public static Transform GetCityRootNode(GameObject codeCity)
        {            
            foreach (Transform child in codeCity.transform)
            {
                if (child.CompareTag(Tags.Node))
                {
                    return child.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the root game object that represents a code city as a whole
        /// along with the settings (layout information etc.). In other words,
        /// we simply return the topmost transform in the game-object hierarchy.
        /// If the given <paramref name="transform"/> is not included in any
        /// other game object, <paramref name="transform"/> will be returned.
        /// 
        /// For reasons of efficiency, we are not checking wether the returned
        /// game object is tagged by Tags.CodeCity. A call may check if this
        /// check is necessary.
        /// </summary>
        /// <param name="transform">transform at which to start the search</param>
        /// <returns>topmost transform in the game-object hierarchy (possibly
        /// <paramref name="transform"/> itself)</returns>
        public static Transform GetCodeCity(Transform transform)
        {
            return transform.root;

            //Transform cursor = transform;
            //while (cursor != null)
            //{
            //    if (cursor.CompareTag(Tags.CodeCity))
            //    {
            //        return cursor;
            //    }
            //    cursor = cursor.parent;
            //}
            //return cursor;
        }

        /// <summary>
        /// Equivalent to: GetCityRootNode(gameObject).GetComponent<NodeRef>.node.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>the root node of the graph or null if there is none</returns>
        public static Node GetCityRootGraphNode(GameObject codeCity)
        {
            Transform transform = GetCityRootNode(codeCity);
            if (transform == null)
            {
                return null;
            }
            else
            {
                NodeRef nodeRef = transform.GetComponent<NodeRef>();
                if (nodeRef == null)
                {
                    return null;
                }
                else
                {
                    return nodeRef.Value;
                }
            }
        }

        /// <summary>
        /// Returns the graph of the root node of <paramref name="codeCity"/> assumed
        /// to represent a code city. Equivalent to: GetCityRootGraphNode(gameObject).ItsGraph.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>the graph represented by <paramref name="codeCity"/> or null</returns>
        public static Graph GetGraph(GameObject codeCity)
        {
            Node root = GetCityRootGraphNode(codeCity);
            if (root == null)
            {
                return null;
            }
            else
            {
                return root.ItsGraph;
            }
        }
    }
}