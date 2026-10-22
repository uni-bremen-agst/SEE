using SEE.SceneManipulation;
using System.Collections;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for marking or unmarking a node via 
    /// network from one client to all others and to the server.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">Unique ID of the node to mark or unmark.</param>
        public MarkNetAction(string nodeID): base(nodeID)
        {
        }

        /// <summary>
        /// Marks or unmarks the node on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.ToggleMark(Find(GraphElementID));
        }
    }
}