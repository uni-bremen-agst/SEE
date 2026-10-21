
using UnityEngine;

using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for marking or unmarking nodes via network
    /// from one client to all others and to the server.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">The ID for the node to mark or unmark.</param>
        public MarkNetAction
            (string nodeID)
            : base(nodeID)
        {
        }

        /// <summary>
        /// Toggles the marking state of the node on each receiving client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);

            if (node != null)
            {
                GameNodeMarker.ToggleMark(node);
            }
        }
    }
}
