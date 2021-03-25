using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates a new edge through the network on each client.
    /// </summary>
    public class AddEdgeNetAction : Net.AbstractAction
    {
        /// <summary>
        /// The id of the gameObject from which the edge should be drawn (source node).
        /// </summary>
        public string FromId;

        /// <summary>
        /// The id of the gameObject to which the edge should be drawn (target node).
        /// </summary>
        public string ToId;

        /// <summary>
        /// The unique of the edge. May be empty or null, in which case a random
        /// unique ID will be create on the client side.
        /// </summary>
        public string EdgeID;

        /// <summary>
        /// Constructs an AddEdgeNetAction.
        /// </summary>
        /// <param name="fromId">The id of the gameObject from which the edge should be drawn</param>
        /// <param name="toId">The id of the gameObject to which the edge should be drawn</param>
        public AddEdgeNetAction(string fromId, string toId, string edgeID)
        {
            this.FromId = fromId;
            this.ToId = toId;
            this.EdgeID = edgeID;
        }

        /// <summary>
        /// Stuff to execute on the Server. Nothing to be done here.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates the new edge on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject fromGO = GameObject.Find(FromId);
                if (fromGO)
                {
                    GameObject toGO = GameObject.Find(ToId);
                    if (toGO)
                    {
                        Transform codeCity = SceneQueries.GetCodeCity(fromGO.transform);
                        if (codeCity)
                        {
                            if (codeCity.gameObject.TryGetComponentOrLog(out SEECity city))
                            {
                                try
                                {
                                    city.Renderer.DrawEdge(fromGO, toGO, EdgeID);
                                }

                                catch (Exception e)
                                {
                                    Debug.LogError($"The new edge from {fromGO.name} to {toGO.name} could not be created: {e.Message}.\n");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError($"Game node named {FromId} is not contained in a code city.\n");
                        }
                    }
                    else
                    {
                        Debug.LogError($"There is no game node named {ToId}.\n");
                    }
                }
                else
                {
                    Debug.LogError($"There is no game node named {FromId}.\n");
                }
            }
        }
    }
}