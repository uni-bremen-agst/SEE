using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;

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
        /// The ID of the GameObject to which the edge should be drawn (target node).
        /// </summary>
        public string EdgeId;

        /// <summary>
        /// The ID of the GameObject to which the edge should be drawn (target node).
        /// </summary>
        public string Type;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fromID">ID of the source node of the edge</param>
        /// <param name="toID">ID for target node of the edge</param>
        public AcceptDivergenceNetAction(string fromId, string toId, string edgeId, string type)
            : base()
        {
            FromId = fromId;
            ToId = toId;
            EdgeId = edgeId;
            Type = type;
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
                        fromGO.TryGetNode(out Node fromDM);
                        toGO.TryGetNode(out Node toDM);

                        // create edge
                        var edgeToPropagate = new Edge(fromDM, toDM, Type);
                        edgeToPropagate.ID = EdgeId;

                        // add the new edge to the architecture graph
                        if (fromDM.ItsGraph is ReflexionGraph graph)
                            graph.AddToArchitecture(edgeToPropagate);

                        // add the new edge to the (game)
                        GameObject edgeGameObject = GameEdgeAdder.Draw(edgeToPropagate);
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
