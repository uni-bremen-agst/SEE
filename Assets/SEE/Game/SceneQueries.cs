using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// Provides queries on game objects in the current scence at run-time.
    /// </summary>
    internal static class SceneQueries
    {
        private const string ArchGOName = "Architecture";
        private const string ImplGOName = "Implementation";
        private const string mappGOName = "Mapping";

        private static SEECity arch = null;
        private static SEECity impl = null;
        private static SEECity mapp = null;

        // TODO(torben): these are very similar to FindImplementation() etc. below @SuperSimilarQuery
        public static SEECity GetArch()
        {
            if (!arch)
            {
                SEECity[] cities = Object.FindObjectsOfType<SEECity>();
                foreach (SEECity city in cities)
                {
                    if (city.gameObject.name.Equals(ArchGOName))
                    {
                        arch = city;
                        break;
                    }
                }
            }
            return arch;
        }

        public static SEECity GetImpl()
        {
            if (!impl)
            {
                SEECity[] cities = Object.FindObjectsOfType<SEECity>();
                foreach (SEECity city in cities)
                {
                    if (city.gameObject.name.Equals(ImplGOName))
                    {
                        impl = city;
                        break;
                    }
                }
            }
            return impl;
        }

        public static SEECity GetMapp()
        {
            if (!mapp)
            {
                SEECity[] cities = Object.FindObjectsOfType<SEECity>();
                foreach (SEECity city in cities)
                {
                    if (city.gameObject.name.Equals(mappGOName))
                    {
                        mapp = city;
                        break;
                    }
                }
            }
            return mapp;
        }

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

        /// <summary>
        /// Returns the roots of all graphs currently referenced by any of the <paramref name="nodeRefs"/>.
        /// </summary>
        /// <param name="nodeRefs">references to nodes in any graphs whose roots are to be returned</param>
        /// <returns>all root nodes of the graphs containing any node referenced in <paramref name="nodeRefs"/></returns>
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
        /// Returns first child of <paramref name="codeCity"/> tagged by Tags.Node. 
        /// If <paramref name="codeCity"/> is a node representing a code city,
        /// the result is considered the root of the graph.
        /// </summary>
        /// <param name="codeCity">object representing a code city (generally tagged by Tags.CodeCity)</param>
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
        /// we simply return the top-most transform in the game-object hierarchy.
        /// That top-most object must be tagged by Tags.CodeCity. If it is,
        /// it will be returned. If not, null will be returned.
        /// </summary>
        /// <param name="transform">transform at which to start the search</param>
        /// <returns>top-most transform in the game-object hierarchy tagged by 
        /// Tags.CodeCity or null</returns>
        public static Transform GetCodeCity(Transform transform)
        {
            Transform result = transform;
            if (PlayerSettings.GetInputType() == PlayerInputType.HoloLensPlayer)
            {
                // If the MRTK is enabled, the cities will be part of a CityCollection, so we can't simply use the root.
                // In this case, we actually have to traverse the tree up until the Tags match.

                while (result != null)
                {
                    if (result.CompareTag(Tags.CodeCity))
                    {
                        return result;
                    }
                    result = result.parent;
                }
                return result;
            }
            result = transform.root;

            if (result.CompareTag(Tags.CodeCity))
            {
                return result;
            }
            else
            {
                return null;
            }
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
            else if (transform.TryGetComponent(out NodeRef nodeRef))
            {
                return nodeRef.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the graph of the root node of <paramref name="codeCity"/> assumed
        /// to represent a code city. Equivalent to: GetCityRootGraphNode(gameObject).ItsGraph.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>the graph represented by <paramref name="codeCity"/> or null</returns>
        public static Graph GetGraph(this GameObject codeCity)
        {
            Node root = GetCityRootGraphNode(codeCity);
            return root?.ItsGraph;
        }

        /// <summary>
        /// Finds the implementation city in the scene.
        /// </summary>
        /// <returns>The implementation city of the scene.</returns>
        public static SEECity FindImplementation() // @SuperSimilarQuery
        {
            SEECity result = null;
            SEECity[] cities = Object.FindObjectsOfType<SEECity>();
            foreach (SEECity city in cities)
            {
                if (city.gameObject.name.Equals("Implementation"))
                {
#if UNITY_EDITOR
                    Assert.IsNull(result, "There must be exactly one implementation city!");
#endif
                    result = city;
#if !UNITY_EDITOR
                    break;
#endif
                }
            }
            return result;
        }

        /// <summary>
        /// Finds the architecture city in the scene.
        /// </summary>
        /// <returns>The architecture city of the scene.</returns>
        public static SEECity FindArchitecture() // @SuperSimilarQuery
        {
            SEECity result = null;
            SEECity[] cities = Object.FindObjectsOfType<SEECity>();
            foreach (SEECity city in cities)
            {
                if (city.gameObject.name.Equals("Architecture"))
                {
#if UNITY_EDITOR
                    Assert.IsNull(result, "There must be exactly one architecture city!");
#endif
                    result = city;
#if !UNITY_EDITOR
                    break;
#endif
                }
            }
            return result;
        }
    }
}