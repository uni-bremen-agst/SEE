using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Replicated the marking of a graph node on the other clients.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// The desired marking state. Public so that network serialization includes it.
        /// </summary>
        public bool Marked;

        /// <summary>
        /// Created a request to set a node's marking.
        /// </summary>
        /// <param name="nodeID">The ID of the game node.</param>
        /// <param name="marked">Whether the node should be marked.</param>
        public MarkNetAction(string nodeID, bool marked) : base(nodeID)
        {
            Marked = marked;
        }

        /// <summary>
        /// Applied the requested state on a remote client, including during history replay.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
        }
    }
}