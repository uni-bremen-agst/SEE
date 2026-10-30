using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for marking or unmarking
    /// a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// The desired marking state. Public so that network serialization includes it.
        /// </summary>
        public bool Marked;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="newNodeID">The ID of the game node.</param>
        /// <param name="marked">Whether the node should be marked.</param>
        public MarkNetAction(string newNodeID, bool marked) : base(newNodeID)
        {
            Marked = marked;
        }

        /// <summary>
        /// Applies marking or removes it for each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
        }
    }
}
