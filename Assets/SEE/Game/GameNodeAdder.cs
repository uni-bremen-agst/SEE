using SEE.DataModel.DG;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Game
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
            string ID = string.IsNullOrEmpty(nodeID) ? Guid.NewGuid().ToString() : nodeID;
            return new Node()
            {
                ID = ID,
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
                graph.AddNode(node);
                parent.AddChild(node);
            }
        }

        /// <summary>
        /// Creates and returns a new game node as a child of <paramref name="parent"/> at the
        /// given <paramref name="position"/> with the given <paramref name="worldSpaceScale"/>.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference.
        /// </summary>
        /// <param name="parent">parent of the new node</param>
        /// <param name="position">the position in world space for the center point of the new game node</param>
        /// <param name="worldSpaceScale">the scale in world space of the new game node</param>
        /// <param name="nodeID">the unique ID of the new node; if null or empty, a random ID will be used</param>
        /// <returns>new child game node or null if none could be created</returns>
        /// <exception cref="Exception">thrown if <paramref name="parent"/> is not contained in a code city</exception>
        public static GameObject AddChild(GameObject parent, Vector3 position, Vector3 worldSpaceScale, string nodeID = null)
        {
            SEECity city = parent.ContainingCity();
            if (city != null)
            {
                bool wasLeaf = parent.IsLeaf();
                Node node = NewGraphNode(nodeID);
                AddNodeToGraph(parent.GetNode(), node);

                if (wasLeaf)
                {
                    // Note: new graph node must already be a child of parent in the
                    // graph, so that the following call works.
                    city.Renderer.RedrawAsInnerNode(parent);
                    // We need to make sure that the new child always fits into the
                    // parent's area. We should not center the new child within its
                    // parent if there are incoming/outgoing edges because they
                    // origin at the parent's center, too. That would then be misleading.
                    // We know that parent was a leaf, so the new child cannot collide
                    // with any child of parent (because none exists).
                    position = FindPlace(parent.transform.position, position);

                    // If parent has incoming/outgoing edges, they need to be adjusted
                    // because quite likely, the height of parent has changed.
                    GameEdgeMover.MoveAllConnectingEdgesOfNode(parent);
                }

                GameObject result = city.Renderer.DrawLeafNode(node);
                result.transform.localScale = worldSpaceScale;
                result.transform.position = new Vector3(position.x, parent.transform.position.y + worldSpaceScale.y / 2, position.z);
                result.transform.SetParent(parent.transform);
                Portal.SetPortal(city.gameObject, gameObject: result);
                return result;
            }
            else
            {
                throw new Exception($"Parent node {parent.name} is not contained in a code city.");
            }
        }

        /// <summary>
        /// Returns the position inbetween <paramref name="start"/> and <paramref name="end"/>.
        /// More precisely, let L be the line from <paramref name="start"/> to <paramref name="end"/>.
        /// Then the point on L is returned whose distance to <paramref name="start"/> equals the
        /// distance to <paramref name="end"/>.
        /// </summary>
        /// <param name="start">start position</param>
        /// <param name="end">end position</param>
        /// <returns>mid point inbetween <paramref name="start"/> and <paramref name="end"/></returns>
        private static Vector3 FindPlace(Vector3 start, Vector3 end)
        {
            return start + 0.5f * (end - start);
        }
    }
}
