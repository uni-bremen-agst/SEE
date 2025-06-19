using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Creates new game objects representing graph nodes or deleting these again,
    /// respectively.
    /// </summary>
    public static class GameNodeAdder
    {
        /// <summary>
        /// Creates and returns a new graph node with a random unique ID,
        /// an empty source name, and an unknown node type. This node is
        /// not yet in any graph.
        /// </summary>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, an empty ID will be used</param>
        /// <returns>new graph node</returns>
        private static Node NewGraphNode(string nodeID)
        {
            string id = string.IsNullOrEmpty(nodeID) ? Guid.NewGuid().ToString() : nodeID;
            return new Node()
            {
                ID = id,
                SourceName = string.Empty,
                Type = Graph.UnknownType
            };
        }

        /// <summary>
        /// Adds a <paramref name="node"/> as a child of <paramref name="parent"/> to the
        /// graph containing <paramref name="parent"/> with a random unique ID.
        ///
        /// If node has no ID yet (null or empty), a random unique ID will be used. If it has
        /// an ID, that ID will be kept. In case this ID is not unique, an exception will
        /// be thrown.
        ///
        /// Precondition: <paramref name="parent"/> must not be null, neither may
        /// <paramref name="parent"/> and <paramref name="node"/> be equal; otherwise an exception
        /// will be thrown.
        /// </summary>
        /// <param name="parent">The node that should be the parent of <paramref name="node"/></param>
        /// <param name="node">The node to add to the graph</param>
        private static void AddNodeToGraph(Node parent, Node node)
        {
            if (parent == null)
            {
                throw new Exception("Parent must not be null.");
            }
            else if (parent == node)
            {
                throw new Exception("Node must not be its own parent.");
            }
            else
            {
                Graph graph = parent.ItsGraph;
                if (graph == null)
                {
                    throw new Exception("Parent must be in a graph.");
                }
                if (string.IsNullOrEmpty(node.ID))
                {
                    // Loop until the node.ID is unique within the graph.
                    node.ID = Guid.NewGuid().ToString();
                    while (graph.GetNode(node.ID) != null)
                    {
                        node.ID = Guid.NewGuid().ToString();
                    }
                }
                // Note: ReflexionGraph.AddNode(node) determines the subgraph where node should be
                // added via its parent. That means, the parent of node must be set before it can
                // be called.
                parent.AddChild(node);
                graph.AddNode(node);
            }
        }

        /// <summary>
        /// Creates and returns a new game node as a child of <paramref name="parent"/> at the
        /// given <paramref name="worldSpacePosition"/> with the given <paramref name="worldSpaceScale"/>.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference
        /// and must be contained in a code city.
        ///
        /// Postcondition: The returned child is an immediate child of <paramref name="parent"/> in the
        /// game object hierarchy and in the underlying graph.
        /// </summary>
        /// <param name="parent">parent of the new node</param>
        /// <param name="worldSpacePosition">the position in world space for the center point of the new game node</param>
        /// <param name="worldSpaceScale">the scale in world space of the new game node</param>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, a random ID will be used</param>
        /// <returns>new child game node>/returns>
        /// <exception cref="Exception">thrown if <paramref name="parent"/> has no valid node reference
        /// or is not contained in a code city</exception>
        public static GameObject AddChild(GameObject parent, Vector3 worldSpacePosition, Vector3 worldSpaceScale, string nodeID = null)
        {
            GameObject result = AddChild(parent, nodeID);
            // Resetting the parent to null temporarily so that there is no difference between
            // local scale and world-space scale.
            result.transform.SetParent(null);
            // result is just created, hence, we do not need a NodeOperator to position and scale it.
            result.transform.position = worldSpacePosition;
            result.transform.localScale = worldSpaceScale;
            result.transform.SetParent(parent.transform);
            return result;
        }

        /// <summary>
        /// Creates and returns a new game node as a child of <paramref name="parent"/>.
        /// The world-space position and scale of the result will be the world-space
        /// position and scale of <paramref name="parent"/>.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference
        /// and must be contained in a code city.
        ///
        /// Postcondition: The returned child is an immediate child of <paramref name="parent"/> in the
        /// game object hierarchy and in the underlying graph.
        /// </summary>
        /// <param name="parent">parent of the new node</param>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, a random ID will be used</param>
        /// <returns>new child game node>/returns>
        /// <exception cref="Exception">thrown if <paramref name="parent"/> has no valid node reference
        /// or is not contained in a code city</exception>
        public static GameObject AddChild(GameObject parent, string nodeID = null)
        {
            SEECity city = parent.ContainingCity() as SEECity;
            if (city != null)
            {
                Node node = NewGraphNode(nodeID);
                city.CheckAndAddUnknownNodeType();
                AddNodeToGraph(parent.GetNode(), node);
                GameObject result = city.Renderer.DrawNode(node);
                result.transform.position = parent.transform.position;
                result.transform.localScale = parent.transform.lossyScale;
                result.transform.SetParent(parent.transform);
                Portal.SetPortal(city.gameObject, gameObject: result);
                return result;
            }
            else
            {
                throw new Exception($"Parent node {parent.FullName()} is not contained in a code city.");
            }
        }
    }
}
