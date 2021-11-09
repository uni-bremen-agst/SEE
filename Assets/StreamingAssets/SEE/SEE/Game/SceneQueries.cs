using System;
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
                        Debug.LogWarning($"Game node {go.name} has a null node reference.\n");
                    }
                }
                else
                {
                    Debug.LogWarning($"Game node {go.name} without node reference.\n");
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
        /// Returns the farthest ancestor in the game-object hierarchy that is tagged by
        /// <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="cityChildTransform">The child transform, to find the root for.</param>
        /// <returns>The root transform of given child, so the highest transform with the tag
        /// <see cref="Tags.Node"/>.</returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="cityChildTransform"/> is <c>null</c></exception>
        public static Transform GetCityRootTransformUpwards(Transform cityChildTransform)
        {
            if (cityChildTransform == null)
            {
                throw new ArgumentNullException();
            }
            Transform result = cityChildTransform;
            while (result.parent != null && result.parent.CompareTag(Tags.Node))
            {
                result = result.parent;
            }
            return result;
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
        /// Retrieves the game object representing a node with the given <paramref name="nodeID"/>.
        ///
        /// Note: This is an expensive operation as it traverses all objects in the scene.
        /// FIXME: We may need to cache all this information in look up tables for better
        /// performance.
        ///
        /// Precondition: Such a game object actually exists.
        /// </summary>
        /// <param name="nodeID">the unique ID of the node to be retrieved</param>
        /// <returns>the game object representing the node with the given <paramref name="nodeID"/></returns>
        /// <exception cref="Exception">thrown if there is no such object</exception>
        public static GameObject RetrieveGameNode(string nodeID)
        {
            foreach (GameObject gameNode in AllGameNodesInScene(true, true))
            {
                if (gameNode.name == nodeID)
                {
                    return gameNode;
                }
            }
            throw new Exception($"Node named {nodeID} not found.");
        }

        /// <summary>
        /// Retrieves the game object representing the given <paramref name="node"/>.
        ///
        /// Note: This is an expensive operation as it traverses all objects in the scene.
        /// FIXME: We may need to cache all this information in look up tables for better
        /// performance.
        ///
        /// Preconditions:
        ///   (1) <paramref name="node"/> is not null.
        ///   (2) Such a game object actually exists.
        /// </summary>
        /// <param name="node">the node to be retrieved</param>
        /// <returns>the game object representing the given <paramref name="node"/></returns>
        /// <exception cref="Exception">thrown if there is no such object</exception>
        public static GameObject RetrieveGameNode(Node node)
        {
            return RetrieveGameNode(node.ID);
        }

        /// <summary>
        /// Retrieves the game object representing the node referenced by the given <paramref name="nodeRef"/>.
        ///
        /// Note: This is an expensive operation as it traverses all objects in the scene.
        /// FIXME: We may need to cache all this information in look up tables for better
        /// performance.
        ///
        /// Preconditions:
        /// (1) <paramref name="nodeRef"/> must reference a valid node.
        /// (2) Such a game object actually exists.
        /// </summary>
        /// <param name="nodeRef">a reference to the node to be retrieved</param>
        /// <returns>the game object representing the node referenced by the given <paramref name="nodeRef"/></returns>
        /// <exception cref="Exception">thrown if there is no such object</exception>
        public static GameObject RetrieveGameNode(NodeRef nodeRef)
        {
            return RetrieveGameNode(nodeRef.Value);
        }

        /// <summary>
        /// Returns the first game object in the current scene with the given <paramref name="id"/>.
        /// Will also return inactive game objects. If no such object exists, null will be returned.
        ///
        /// Note: This method will descend only in the root nodes tagged by <see cref="Tags.CodeCity"/>.
        /// Those roots themselves will not be considered.
        /// </summary>
        /// <param name="id">id of the game object to be found</param>
        /// <returns>found game object or null</returns>
        public static GameObject Find(string id)
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // GetRootGameObjects() yields also inactive game objects
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root.CompareTag(Tags.CodeCity))
                {
                    GameObject ancestor = root.Ancestor(id);
                    if (ancestor != null)
                    {
                        return ancestor;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all game objects in the current scene having a name contained in <paramref name="gameObjectIDs"/>.
        /// Will also return inactive game objects.
        ///
        /// Note: This method will descend only in the root nodes tagged by <see cref="Tags.CodeCity"/>.
        /// Those roots themselves will not be included.
        /// </summary>
        /// <param name="gameObjectIDs">list of names any of the game objects to be retrieved should have</param>
        /// <returns>found game objects</returns>
        public static ISet<GameObject> Find(ISet<string> gameObjectIDs)
        {
            ISet<GameObject> result = new HashSet<GameObject>();
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // GetRootGameObjects() yields also inactive game objects
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root.CompareTag(Tags.CodeCity))
                {
                    result.UnionWith(root.Ancestors(gameObjectIDs));
                }
            }
            return result;
        }

        //------------------------------------------------------------
        // Queries necessary in the context of the reflexion analysis.
        //------------------------------------------------------------

        /// <summary>
        /// Cached implementation city
        /// </summary>
        private static SEECity cachedImplementation = null;

        /// <summary>
        /// Cached architecture city
        /// </summary>
        private static SEECity cachedArchitecture = null;

        /// <summary>
        /// Cached mapping city
        /// </summary>
        private static SEECity cachedMapping = null;

        /// <summary>
        /// Finds the implementation city in the scene. The city may be cached.
        /// </summary>
        /// <returns>The implementation city of the scene.</returns>
        public static SEECity FindImplementation()
        {
            if (!cachedImplementation)
            {
                cachedImplementation = FindSEECity("Implementation");
            }
            return cachedImplementation;
        }

        /// <summary>
        /// Finds the architecture city in the scene. The city may be cached.
        /// </summary>
        /// <returns>The architecture city of the scene.</returns>
        public static SEECity FindArchitecture()
        {
            if (!cachedArchitecture)
            {
                cachedArchitecture = FindSEECity("Architecture");
            }
            return cachedArchitecture;
        }

        /// <summary>
        /// Finds the mapping city in the scene. The city may be cached.
        /// </summary>
        /// <returns>The mapping city of the scene.</returns>
        public static SEECity FindMapping()
        {
            if (!cachedMapping)
            {
                cachedMapping = FindSEECity("Mapping");
            }
            return cachedMapping;
        }

        /// <summary>
        /// Finds the <see cref="SEECity"/> of <see cref="GameObject"/> with name
        /// <paramref name="name"/>. As this method does not cache anything, this call is quite
        /// heavy and should not be called too often.
        /// </summary>
        /// <param name="name">The name of the object with city component attached.</param>
        /// <returns>The found city or <code>null</code>, if no such city exists.</returns>
        private static SEECity FindSEECity(string name)
        {
            SEECity result = null;
            SEECity[] cities = UnityEngine.Object.FindObjectsOfType<SEECity>();
            foreach (SEECity city in cities)
            {
                if (city.gameObject.name.Equals(name))
                {
#if UNITY_EDITOR
                    Assert.IsNull(result, "There must be exactly one city named " + name + "!");
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
