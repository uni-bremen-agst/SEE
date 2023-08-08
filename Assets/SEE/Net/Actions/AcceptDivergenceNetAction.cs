using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for adding a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class AcceptDivergenceNetAction : AbstractNetAction
    {
        /// <summary>
        /// The ID of the GameObject from which the edge should be drawn (source node).
        /// </summary>
        public string FromId;

        /// <summary>
        /// The ID of the GameObject to which the edge should be drawn (target node).
        /// </summary>
        public string ToId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fromID">ID of the source node of the edge</param>
        /// <param name="toID">ID for target node of the edge</param>
        public AcceptDivergenceNetAction(string fromId, string toId)
            : base()
        {
            FromId = fromId;
            ToId = toId;
        }

        /// <summary>
        /// Things to execute on the server (none for this
        /// class). Necessary because it is abstract in the
        /// superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates a new GameObject on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject fromGO = GraphElementIDMap.Find(FromId);
                if (fromGO)
                {
                    GameObject toGO = GraphElementIDMap.Find(ToId);
                    if (toGO)
                    {
                        GameEdgeAdder.Add(fromGO, toGO, "Source_Dependency");
                    }
                    else
                    {
                        Debug.LogError($"There is no game node named {ToId} for the target.\n");
                    }
                }
                else
                {
                    Debug.LogError($"There is no game node named {FromId} for the source.\n");
                }
            }
        }
    }
}
