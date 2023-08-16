using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for adding a specific edge via
    /// network from one client to all others and to the server, in
    /// order to solve a divergence.
    /// </summary>
    public class AcceptDivergenceNetAction : AbstractNetAction
    {
        /// <summary>
        /// The ID of the Node's GameObject from which the edge should be
        /// drawn (source node).
        /// </summary>
        public string FromId;

        /// <summary>
        /// The ID of the Node's GameObject to which the edge should be drawn
        /// (target node).
        /// </summary>
        public string ToId;

        /// <summary>
        /// The ID of the server's edge to ensure they match
        /// between server and clients.
        /// </summary>
        public string EdgeId;

        /// <summary>
        /// The type of the edge that should be created in order to
        /// solve the divergence.
        /// </summary>
        public string Type;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fromId">ID of the source node of the edge</param>
        /// <param name="toId">ID for target node of the edge</param>
        /// <param name="edgeId">ID of the edge to be propagated to the clients</param>
        /// <param name="type">the type of the created edge</param>
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
                GameObject fromGO = Find(FromId);
                if (fromGO.TryGetNode(out Node fromNode))
                {
                    GameObject toGO = Find(ToId);
                    if (toGO.TryGetNode(out Node toNode))
                    {
                        // FIXME: This code is very similar to AcceptDivergenceAction.CreateEdge.
                        // NetActions must be as dump as possible. All logic shared by these
                        // and their corresponding Action must be extracted to a separate class.

                        // create the edge beforehand (to change its ID)
                        Edge edgeToPropagate = new(fromNode, toNode, Type)
                        {
                            // change the edges ID before adding it to a graph
                            ID = EdgeId
                        };

                        // add the already created edge to the architecture graph
                        if (fromNode.ItsGraph is ReflexionGraph graph)
                        {
                            graph.AddToArchitecture(edgeToPropagate);
                        }

                        // (re)draw the new edge
                        GameEdgeAdder.Draw(edgeToPropagate);
                    }
                    else
                    {
                        Debug.LogError($"Game node {ToId} has not graph node attached.\n");
                    }
                }
                else
                {
                    Debug.LogError($"Game node {FromId} has not graph node attached.\n");
                }
            }
        }
    }
}
