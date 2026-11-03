using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for marking or unmarking a node via network from one client
    /// to all others and to the server.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// Whether the node should be marked (true) or unmarked (false) afterwards.
        /// </summary>
        public bool Mark;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">Unique ID of the node to be marked or unmarked.</param>
        /// <param name="mark">Whether the node should be marked (true) or unmarked (false).</param>
        public MarkNetAction(string nodeID, bool mark) : base(nodeID)
        {
            Mark = mark;
        }

        /// <summary>
        /// Marks or unmarks the node on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);
            if (Mark)
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
