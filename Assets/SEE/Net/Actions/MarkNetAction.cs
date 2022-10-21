using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class is responsible for marking a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The GameObject that is targeted for marking.
        /// </summary>
        public GameObject TargetNode;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="targetNode">the node that was targeted for marking.</param>
        public MarkNetAction
        (GameObject targetNode)
        : base()
        {
            this.TargetNode = targetNode;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Tries marking on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject markerSphere = GameNodeMarker.TryMarking(TargetNode);
            }
        }
    }
}
