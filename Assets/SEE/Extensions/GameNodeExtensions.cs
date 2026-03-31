using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GraphElementRefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Extension methods for game nodes. A game node is a <see cref="GameObject"/>
    /// representing a <see cref="SEE.DataModel.DG.Node"/>.
    /// </summary>
    internal static class GameNodeExtensions
    {
        /// <summary>
        /// Returns the first immediate child of <paramref name="gameObject"/> that
        /// is a graph node, i.e., has a <see cref="NodeRef"/> attached to it
        /// (checked by predicate <see cref="IsNode(GameObject)"/>) or null if there
        /// is none.
        /// </summary>
        /// <param name="gameObject">The game object whose child is to be retrieved.</param>
        /// <returns>First immediate child representing a node or null if there is none.</returns>
        /// <remarks>Applicable to game nodes only.</remarks>
        public static GameObject FirstChildNode(this GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.gameObject.IsNode())
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents a leaf in the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is a leaf.</param>
        /// <returns>True if <paramref name="gameNode"/> represents a leaf in the graph.</returns>
        /// <remarks>Applicable to game nodes only.</remarks>
        public static bool IsLeaf(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsLeaf() ?? false;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents the root of the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is a root node.</param>
        /// <returns>True if <paramref name="gameNode"/> represents a root in the graph.</returns>
        /// <remarks>Applicable to game nodes only.</remarks>
        public static bool IsRoot(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsRoot() ?? false;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> represents the implementation or architecture root of
        /// the graph.
        ///
        /// Precondition: <paramref name="gameNode"/> has a <see cref="NodeRef"/> component
        /// attached to it that is a valid graph node reference.
        /// </summary>
        /// <param name="gameNode">Game object representing a Node to be queried whether it is an implementation or architecture root.</param>
        /// <returns>True if <paramref name="gameNode"/> represents an implementation or architecture root in the graph.</returns>
        /// <remarks>Applicable to game nodes only.</remarks>
        public static bool IsArchitectureOrImplementationRoot(this GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>()?.Value?.IsArchitectureOrImplementationRoot() ?? false;
        }

        /// <summary>
        /// Returns the world-space y position of the roof of this <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">Game object whose roof has to be determined.</param>
        /// <returns>World-space y position of the roof of this <paramref name="gameNode"/>.</returns>
        /// <remarks>This does not consider the position of descendants if there are any.
        /// Consider <see cref="GetTop(GameObject, Func{Transform, bool})"/> if you want to
        /// take descendants into account, too.</remarks>
        public static float GetRoof(this GameObject gameNode)
        {
            return gameNode.transform.position.y + gameNode.WorldSpaceSize().y / 2.0f;
        }

        /// <summary>
        /// Returns the maximal world-space position (y co-ordinate) of the roof of
        /// this <paramref name="gameNode"/> or any of its active descendants.
        /// Unlike <see cref="GetRoof(GameObject)"/>, this method recurses into
        /// the game-object hierarchy rooted by <paramref name="gameNode"/>.
        ///
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameNode">Game object whose height has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>World-space position of the roof of this <paramref name="gameNode"/>
        /// or any of its active descendants.</returns>
        public static float GetMaxY(this GameObject gameNode, Func<Transform, bool> filterTransform = null)
        {
            float result = float.NegativeInfinity;
            filterTransform ??= _ => true;
            Recurse(gameNode, ref result);
            return result;

            void Recurse(GameObject root, ref float max)
            {
                float roof = root.GetRoof();
                if (max < roof)
                {
                    max = roof;
                }

                foreach (Transform child in root.transform)
                {
                    if (child.gameObject.activeInHierarchy && filterTransform(child))
                    {
                        Recurse(child.gameObject, ref max);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the maximal world-space center position of the hull of
        /// this <paramref name="gameNode"/>. The hull includes <paramref name="gameNode"/>
        /// and any of its active descendants.
        /// Unlike <see cref="GetRoof(GameObject)"/>, this method recurses into
        /// the game-object hierarchy rooted by <paramref name="gameNode"/>.
        ///
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameNode">Game object whose center top has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>World-space position of the center top of the hull of this <paramref name="gameNode"/>.
        /// </returns>
        /// <remarks>The result is in world space of <see cref="gameObject"/>. If your are interested
        /// in local space, use <see cref="GetRelativeTop(GameObject, Func{Transform, bool})"/> instead.</remarks>
        public static Vector3 GetTop(this GameObject gameNode, Func<Transform, bool> filterTransform = null)
        {
            Vector3 result = gameNode.transform.position;
            result.y = gameNode.GetMaxY(filterTransform);
            return result;
        }

        /// <summary>
        /// Returns the maximal local-space center position of the hull of
        /// this <paramref name="gameNode"/>. The hull includes <paramref name="gameNode"/>
        /// and any of its active descendants.
        /// Note: only descendants that are currently active in the scene are considered.
        /// </summary>
        /// <param name="gameNode">Game object whose center top has to be determined.</param>
        /// <param name="filterTransform">Function returning true for descendant transforms that shall be taken into
        /// account. By default, this is a constant function which always returns true.</param>
        /// <returns>Local-space position of the center top of the hull of this <paramref name="gameNode"/>.
        /// </returns>
        /// <remarks>The result is in local space of <see cref="gameObject"/>. If your are interested
        /// in world space, use <see cref="GetTop(GameObject, Func{Transform, bool})"/> instead.</remarks>
        public static float GetRelativeTop(this GameObject gameNode, Func<Transform, bool> filterTransform = null)
        {
            float top = gameNode.GetMaxY(filterTransform);
            return top - gameNode.transform.position.y;
        }

        /// <summary>
        /// Returns the world-space center position of the ground of this <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">Game object whose ground has to be determined.</param>
        /// <returns>World-space center position of the ground of this <paramref name="gameNode"/>.</returns>
        public static Vector3 GetGroundCenter(this GameObject gameNode)
        {
            Vector3 result = gameNode.transform.position;
            result.y -= gameNode.WorldSpaceSize().y / 2.0f;
            return result;
        }

        /// <summary>
        /// Sets the scale of this <paramref name="gameObject"/> to <paramref name="worldScale"/> independent from
        /// the local scale of its parent.
        /// </summary>
        /// <param name="gameObject">Object whose scale should be set.</param>
        /// <param name="worldScale">The new scale in world space.</param>
        /// <param name="animate">If true and <paramref name="gameObject"/> is a graph node,
        /// a <see cref="Game.Operator.NodeOperator"/> will be used to animate the scaling; otherwise the
        /// scale of <paramref name="gameObject"/> is set immediately without any animation.</param>
        /// <remarks>Is intended primarily for game nodes but also applicable to other kinds of
        /// <see cref="UnityEngine.GameObject"/>s.</remarks>
        public static void SetAbsoluteScale(this GameObject gameObject, Vector3 worldScale, bool animate = true)
        {
            Transform parent = gameObject.transform.parent;
            gameObject.transform.parent = null;
            if (animate && gameObject.HasNodeRef())
            {
                NodeOperator @operator = gameObject.NodeOperator();
                @operator.ScaleTo(worldScale, 0f);
            }
            else
            {
                gameObject.transform.localScale = worldScale;
            }
            gameObject.transform.parent = parent;
        }

        /// <summary>
        /// Returns true if <paramref name="gameNode"/> has a <see cref="NodeRef"/>
        /// component attached to it that is actually referring to a valid node
        /// (i.e., its Value is not null).
        /// </summary>
        /// <param name="gameNode">The game object whose NodeRef is checked.</param>
        /// <returns>True if <paramref name="gameNode"/> has a <see cref="NodeRef"/>
        /// component attached to it whose node is non-null.</returns>
        public static bool HasNodeRef(this GameObject gameNode)
        {
            return gameNode.TryGetComponent(out NodeRef nodeRef) && nodeRef.Value != null;
        }

        /// <summary>
        /// Returns true if <paramref name="gameNode"/> is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameNode">The game object to check.</param>
        /// <returns>True if <paramref name="gameNode"/> is tagged by <see cref="Tags.Node"/>.</returns>
        public static bool IsNode(this GameObject gameNode)
        {
            return gameNode.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameNode"/>'s <see cref="GameObject.activeSelf"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameNode">The game object to check.</param>
        /// <returns>True if <paramref name="gameNode"/> is an active node.</returns>
        public static bool IsNodeAndActiveSelf(this GameObject gameNode)
        {
            return gameNode.activeSelf && gameNode.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Returns true if <paramref name="gameNode"/>'s <see cref="GameObject.activeInHierarchy"/>
        /// is true and it is tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameNode">The game object to check.</param>
        /// <returns>True if <paramref name="gameNode"/> is an active node.</returns>
        public static bool IsNodeAndActiveInHierarchy(this GameObject gameNode)
        {
            return gameNode.CompareTag(Tags.Node) && gameNode.activeInHierarchy;
        }

        /// <summary>
        /// Retrieves the node reference component, if possible.
        /// </summary>
        /// <param name="gameNode">The game object whose NodeRef is checked.</param>
        /// <param name="nodeRef">The attached NodeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameNode"/> has a <see cref="NodeRef"/>
        /// component attached to it.</returns>
        public static bool TryGetNodeRef(this GameObject gameNode, out NodeRef nodeRef)
        {
            return gameNode.TryGetComponent(out nodeRef);
        }

        /// <summary>
        /// Returns true if <paramref name="gameNode"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null.
        /// </summary>
        /// <param name="gameNode">The game object whose NodeRef is checked.</param>
        /// <param name="node">The node referenced by the attached NodeRef; defined only if this method
        /// returns true.</param>
        /// <returns>True if <paramref name="gameNode"/> has a <see cref="NodeRef"/>
        /// component attached to it that is not null.</returns>
        public static bool TryGetNode(this GameObject gameNode, out Node node)
        {
            node = null;
            if (gameNode.TryGetComponent(out NodeRef nodeRef))
            {
                node = nodeRef.Value;
            }
            return node != null;
        }

        /// <summary>
        /// Returns the graph node represented by this <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameNode">The game object whose Node is requested.</param>
        /// <returns>The correponding graph node (will never be null).</returns>
        /// <exception cref="NullReferenceException">Thrown if <paramref name="gameNode"/> has
        /// no valid <see cref="NodeRef"/> or <see cref="Node"/>.</exception>
        public static Node GetNode(this GameObject gameNode)
        {
            if (gameNode.TryGetComponent(out NodeRef nodeRef))
            {
                if (nodeRef != null)
                {
                    if (nodeRef.Value != null)
                    {
                        return nodeRef.Value;
                    }
                    else
                    {
                        throw new NullReferenceException($"Node referenced by game object {gameNode.name} is null.");
                    }
                }
                else
                {
                    throw new NullReferenceException($"Node reference of game object {gameNode.name} is null.");
                }
            }
            else
            {
                throw new NullReferenceException($"Game object {gameNode.name} has no NodeRef.");
            }
        }

        /// <summary>
        /// Returns the graph containing the node represented by this <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have a <see cref="NodeRef"/>
        /// attached to it referring to a valid node; if not, an exception is raised.
        /// </summary>
        /// <param name="gameNode">The game object whose graph is requested.</param>
        /// <returns>The correponding graph.</returns>
        public static Graph ItsGraph(this GameObject gameNode)
        {
            return gameNode.GetNode().ItsGraph;
        }

        /// <summary>
        /// Returns all active descendants of given <paramref name="gameNode"/> tagged by <see cref="Tags.Node"/>
        /// including <paramref name="gameNode"/> itself.
        /// </summary>
        /// <param name="gameNode">The root of the node hierarchy to be collected.</param>
        /// <returns>All descendants of <paramref name="gameNode"/> including <paramref name="gameNode"/>.</returns>
        public static IList<GameObject> AllDescendants(this GameObject gameNode)
        {
            IList<GameObject> result = new List<GameObject>() { gameNode };
            AllDescendants(gameNode, result);
            return result;
        }

        /// <summary>
        /// Adds all active descendants of <paramref name="gameNode"/> to <paramref name="result"/>
        /// (only if tagged by <see cref="Tags.Node"/>).
        ///
        /// Note: <paramref name="gameNode"/> is assumed to be contained in <paramref name="result"/>
        /// already.
        /// </summary>
        /// <param name="gameNode">The root of the game-object hierarchy to be collected.</param>
        /// <param name="result">Where to add the descendants.</param>
        private static void AllDescendants(GameObject gameNode, IList<GameObject> result)
        {
            foreach (Transform child in gameNode.transform)
            {
                if (child.gameObject.activeInHierarchy && child.gameObject.CompareTag(Tags.Node))
                {
                    result.Add(child.gameObject);
                    AllDescendants(child.gameObject, result);
                }
            }
        }

        /// <summary>
        /// Returns the <see cref="NodeOperator"/> for this <paramref name="gameNode"/>.
        /// If no operator exists yet, it will be added.
        /// If the game object is not a node, an exception will be thrown.
        /// </summary>
        /// <param name="gameNode">The game object whose operator to retrieve.</param>
        /// <returns>The <see cref="NodeOperator"/> responsible for this <paramref name="gameNode"/>.</returns>
        public static NodeOperator NodeOperator(this GameObject gameNode)
        {
            if (gameNode.CompareTag(Tags.Node))
            {
                return gameNode.AddOrGetComponent<NodeOperator>();
            }
            else
            {
                throw new InvalidOperationException($"Cannot get {nameof(NodeOperator)} for game object {gameNode.name} because it is not a node.");
            }
        }
    }
}
