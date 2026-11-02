using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the adding or removing of a mark through the network
    /// </summary>
    internal class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// The node to be marked or unmarked.
        /// </summary>
        public readonly GameObject node;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">The ID for the node to mark (or unmark).</param>
        public MarkNetAction(string nodeID)
            : base(nodeID)
        {
            node = Find(nodeID);
        }

        /// <summary>
        /// Toggles the marking state of the node on each receiving client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.AddMark(node);
        }
    }
}
