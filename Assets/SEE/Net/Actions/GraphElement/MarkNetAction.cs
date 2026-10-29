using SEE.GraphElementRefs;
using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the adding or removing of a mark through the network
    /// </summary>
    internal class MarkNetAction : NodeNetAction
    {

        public string ID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">The ID for the node to mark (or unmark).</param>
        public MarkNetAction(string nodeID)
            : base(nodeID)
        {
            ID = nodeID;
        }

        /// <summary>
        /// Toggles the marking state of the node on each receiving client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(ID);
            GameNodeMarker.AddMark(node);
        }
    }
}
