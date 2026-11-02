using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the desired marking state of a node to other clients.
    /// </summary>
    internal class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Whether the node must be marked. Public for network serialization.
        /// </summary>
        public bool Marked;

        /// <summary>
        /// Creates a network action carrying the desired marking state of a node.
        /// </summary>
        /// <param name="gameNodeID">The unique identifier of the node to mark or unmark.</param>
        /// <param name="marked">True to mark the node; false to unmark it.</param>
        public MarkNetAction(string gameNodeID, bool marked) : base(gameNodeID)
        {
            Marked = marked;
        }

        /// <summary>
        /// Applies the desired marking state on a receiving client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
        }
    }
}
