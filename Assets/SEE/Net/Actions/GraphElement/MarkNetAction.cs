using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates marking or unmarking a node via network to all other clients.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">The ID of the node to be marked.</param>
        public MarkNetAction(string nodeID) : base(nodeID) { }

        /// <summary>
        /// Changes the mark of the node on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject nodeToMark = Find(GraphElementID);
            GameNodeMarker.ToggleMark(nodeToMark);
        }
    }
}
