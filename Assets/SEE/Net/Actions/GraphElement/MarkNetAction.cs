using UnityEngine;

using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Sends the marking or unmarking of a node through the network.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// Whether the node should be marked.
        /// </summary>
        public bool IsMarked;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">Unique ID of the node to mark or unmark.</param>
        /// <param name="isMarked">Whether the node should be marked.</param>
        public MarkNetAction(string nodeID, bool isMarked)
            : base(nodeID)
        {
            IsMarked = isMarked;
        }

        /// <summary>
        /// Marks or unmarks the given node on each receiving client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);
            if (IsMarked)
            {
                GameNodeMarker.Mark(node);
            }
            else
            {
                GameNodeMarker.Unmark(node);
            }
        }
    }
}
