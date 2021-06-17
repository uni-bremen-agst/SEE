using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Creates a new edge through the network on each client.
    /// </summary>
    public class AddEdgeNetAction : AbstractAction
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
        /// The unique id of the edge. May be empty or null, in which case a random
        /// unique ID will be created on the client side.
        /// </summary>
        public string EdgeID;

        /// <summary>
        /// Constructs an AddEdgeNetAction.
        /// </summary>
        /// <param name="fromId">The id of the gameObject from which the edge should be drawn</param>
        /// <param name="toId">The id of the gameObject to which the edge should be drawn</param>
        /// <param name="edgeID">The unique ID of the edge; may be null or empty in which case
        /// a random ID will used</param>
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
                        GameEdgeAdder.Add(fromGO, toGO, EdgeID);
                    }
                    else
                    {
                        Debug.LogError($"There is no game node named {ToId} for the target of new edge {EdgeID}.\n");
                    }
                }
                else
                {
                    Debug.LogError($"There is no game node named {FromId} for the source of new edge {EdgeID}.\n");
                }
            }
        }
    }
}