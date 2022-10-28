using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class is responsible for marking nodes via network from one client to all others and
    /// to the server. This is done by only displaying the spheres on the clients but not storing anything on the server.
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The GameObject that is being marked.
        /// </summary>
        public GameObject Node;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the node that was marked.</param>
        public MarkNetAction
        (GameObject node)
        : base()
        {
            this.Node = node;
        }

        /// <summary>
        /// Not used but necessary.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Executes the marking process on each client
        /// </summary>
        protected override void ExecuteOnClient()
        {
            // If it is not the same client.
            if (!IsRequester())
            {
                GameObject markerSphere = GameNodeMarker.ToggleMark(this.Node);
            }
        }
    }
}
