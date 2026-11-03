using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{

    /// <summary>
    /// Replicates the marking state of a game node to remote clients.
    /// </summary>
    internal class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// The serialized desired state: true to mark the node; false to unmark it.
        /// </summary>
        public bool Marked;

        /// <summary>
        /// Creates a network action that applies the requested marking state.
        /// </summary>
        /// <param name="gameNodeID">The ID of the existing graph node to mark or unmark.</param>
        /// <param name="marked">True to mark the node; false to unmark it.</param>
        internal MarkNetAction(string gameNodeID, bool marked)
            : base(gameNodeID)
        {
            Marked = marked;
        }

        /// <summary>
        /// Applies <see cref="Marked"/> to the node identified by <see cref="GraphElementID"/>.
        /// </summary>
        /// <exception cref="System.Exception">No game object is registered for the target node ID.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The required marker material resource cannot be loaded.</exception>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Marked);
        }
    }
}
