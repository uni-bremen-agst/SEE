using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Creates new game objects representing markers or deleting these again,
    /// respectively. The MarkNetAction is similiar to the GameNodeMarker.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates and returns a new graph marker with a random unique ID,
        /// an empty source name, and an unknown node type. This marker is
        /// not yet in any graph.
        /// </summary>
        /// <param name="markerID">the unique ID of the new node; if null or empty, an empty ID will be used</param>
        /// <returns>new graph node</returns>
        private static Node NewGraphNode(string markerID)
        {
            string ID = string.IsNullOrEmpty(markerID) ? Guid.NewGuid().ToString() : markerID;
            return new Node()
            {
                ID = ID,
                SourceName = string.Empty,
                Type = Graph.UnknownType
            };
        }

        /// <summary>
        /// Marker will be handled like a Node as of the similar behaviour.
        /// Adds a <paramref name="node"/> as a child of <paramref name="parent"/> to the
        /// graph containing <paramref name="parent"/> with a random unique ID.
        ///
        /// If marker has no ID yet (null or empty), a random unique ID will be used. If it has
        /// an ID, that ID will be kept. In case this ID is not unique, an exception will
        /// be thrown.
        ///
        /// Precondition: <paramref name="parent"/> must not be null, neither may
        /// <paramref name="parent"/> and <paramref name="marker"/> be equal; otherwise an exception
        /// will be thrown.
        /// </summary>
        /// <param name="parent">The node that should be the parent of <paramref name="marker"/></param>
        /// <param name="marker">The marker to add to the graph</param>
        private static void AddNodeToGraph(Node parent, Node marker)
        {
            if (parent == null)
            {
                throw new Exception("Parent must not be null.");
            }

            // Asks if there is already an highlighted marker assigned to the node. 
            if (parent.IsMarked == false)
            {
                Graph graph = parent.ItsGraph;
                if (graph == null)
                {
                    throw new Exception("Parent must be in a graph.");
                }

                if (string.IsNullOrEmpty(marker.ID))
                {
                    // Loop until the node.ID is unique within the graph.
                    marker.ID = Guid.NewGuid().ToString();
                    while (graph.GetNode(marker.ID) != null)
                    {
                        marker.ID = Guid.NewGuid().ToString();
                    }
                }

                graph.AddNode(marker);
                parent.AddChild(marker);
                parent.IsMarked = true;
            }
            else
            {
                parent.IsMarked = false;
                //Destroys the marker //FIXME: Doesnt find the ID
                if (marker != null)
                {
                    new DeleteNetAction(marker.ID).Execute();
                    Destroyer.DestroyGameObject(marker.RetrieveGameNode());
                    parent.IsMarked = false;
                }
            }
        }

        /// <summary>
        /// Creates and returns a new game object as a marker as a child of <paramref name="parent"/> at the
        /// given <paramref name="position"/> with the given <paramref name="worldSpaceScale"/>.
        ///
        /// Precondition: <paramref name="parent"/> must have a valid node reference.
        /// </summary>
        /// <param name="parent">parent of the new marker</param>
        /// <param name="position">the position in world space for the center point of the new marker</param>
        /// <param name="worldSpaceScale">the scale in world space of the new game node</param>
        /// <param name="markerID">the unique ID of the new marker; if null or empty, a random ID will be used</param>
        /// <returns>new marker or null if none could be created</returns>
        /// <exception cref="Exception">thrown if <paramref name="parent"/> is not contained in a code city</exception>
        public static GameObject AddChild(GameObject parent, Vector3 position, Vector3 worldSpaceScale,
            string markerID = null)
        {
            SEECity city = parent.ContainingCity() as SEECity;
            if (city != null)
            {
                Node marker = NewGraphNode(markerID);
                AddNodeToGraph(parent.GetNode(), marker);
                position = GameNodeAdder.FindPlace(parent.transform.position, position);

                //create sphere
                GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                result.transform.localScale = worldSpaceScale * 3;
                result.transform.position = new Vector3(
                    position.x,
                    position.y + 0.1f,
                    position.z);
                result.transform.SetParent(parent.transform);
                Portal.SetPortal(city.gameObject, gameObject: result);
                result.GetComponent<Renderer>().material.color = Color.white;
                return result;
            }

            throw new Exception($"Parent node {parent.name} is not contained in a code city.");
        }
    }
}