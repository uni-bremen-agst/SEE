using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides queries on game objects in the current scene at run-time.
    /// </summary>
    internal static class SceneQueries
    {
        /// <summary>
        /// Returns all game objects in the current scene tagged by Tags.Node and having
        /// a valid reference to a graph node.
        /// </summary>
        /// <returns>All game objects representing graph nodes in the scene.</returns>
        public static ICollection<GameObject> AllGameNodesInScene(bool includeLeaves, bool includeInnerNodes)
        {
            List<GameObject> result = new();
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
        /// <returns>All game objects representing graph nodes in the scene.</returns>
        public static List<NodeRef> AllNodeRefsInScene(bool includeLeaves, bool includeInnerNodes)
        {
            List<NodeRef> result = new();
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
        /// <param name="gameNodes">Game nodes whose roots are to be returned.</param>
        /// <returns>All root nodes in the scene.</returns>
        public static List<Node> GetRoots(IEnumerable<GameObject> gameNodes)
        {
            return GetGraphs(gameNodes).SelectMany(graph => graph.GetRoots()).ToList();
        }

        /// <summary>
        /// Returns the roots of all graphs currently referenced by any of the <paramref name="nodeRefs"/>.
        /// </summary>
        /// <param name="nodeRefs">References to nodes in any graphs whose roots are to be returned.</param>
        /// <returns>All root nodes of the graphs containing any node referenced in <paramref name="nodeRefs"/>.</returns>
        public static HashSet<Node> GetRoots(IEnumerable<NodeRef> nodeRefs)
        {
            HashSet<Node> result = new();
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
        /// <param name="gameNodes">Game nodes whose graph is to be returned.</param>
        /// <returns>All graphs in the scene.</returns>
        public static HashSet<Graph> GetGraphs(IEnumerable<GameObject> gameNodes)
        {
            return gameNodes.Select(go => go.GetComponent<NodeRef>().Value.ItsGraph).ToHashSet();
        }

        /// <summary>
        /// Returns the farthest ancestor in the game-object hierarchy that is tagged by
        /// <see cref="Tags.Node"/>.
        /// If <paramref name="cityChildTransform"/> has no parent or if its parent is not tagged by
        /// <see cref="Tags.Node"/>, <paramref name="cityChildTransform"/> will be returned.
        /// </summary>
        /// <param name="cityChildTransform">The child transform, to find the root for.</param>
        /// <returns>The root transform of given child, so the highest transform with the tag
        /// <see cref="Tags.Node"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cityChildTransform"/>
        /// is <c>null</c>.</exception>
        public static Transform GetCityRootTransformUpwards(Transform cityChildTransform)
        {
            if (cityChildTransform == null)
            {
                throw new ArgumentNullException(nameof(cityChildTransform));
            }
            Transform result = cityChildTransform;
            while (result.parent != null && result.parent.CompareTag(Tags.Node))
            {
                result = result.parent;
            }
            return result;
        }

        /// <summary>
        /// Returns all game objects in the current scene having a name contained in <paramref name="gameObjectNames"/>.
        /// Will also return inactive game objects.
        ///
        /// Note: This method is expensive because it will iterate over all game objects in the
        /// scene. Use it wisely.
        /// </summary>
        /// <param name="gameObjectNames">List of names any of the game objects to be retrieved should have.</param>
        /// <returns>Found game objects.</returns>
        public static ISet<GameObject> Find(ISet<string> gameObjectNames)
        {
            ISet<GameObject> result = new HashSet<GameObject>();
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // GetRootGameObjects() yields also inactive game objects
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                result.UnionWith(root.Descendants(gameObjectNames));
            }
            return result;
        }

        /// <summary>
        /// Returns the local player game object.
        /// </summary>
        /// <returns>Local player game object.</returns>
        public static GameObject GetLocalPlayer()
        {
            return WindowSpaceManager.ManagerInstance.gameObject;
        }
    }
}
