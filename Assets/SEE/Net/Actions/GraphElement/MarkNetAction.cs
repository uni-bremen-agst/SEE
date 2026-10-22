using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Broadcasts toggling of the node marker.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">ID of the node whose marking is to be toggled.</param>
        public MarkNetAction(string nodeID) : base(nodeID)
        {
        }

        /// <summary>
        /// Toggles the marking of the node for everyone.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);
            if (node != null)
            {
                GameNodeMarker.ToggleMarker(node);
            }
        }
    }
}
