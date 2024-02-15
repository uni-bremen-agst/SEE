using SEE.DataModel.DG;
using UnityEngine;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Game.SceneManipulation;

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
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates a new GameObject on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            AcceptDivergence.Accept(Find(FromId), Find(ToId), Type, EdgeId);
        }
    }
}
