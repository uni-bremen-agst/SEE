using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for marking a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkNetAction"/> class.
        /// </summary>
        /// <param name="gameNodeID">The ID of the node to mark.</param>
        public MarkNetAction(string gameNodeID) : base(gameNodeID)
        {
        }

        /// <summary>
        /// Creates a new GameObject  on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.Toggle(Find(GraphElementID));
        }
    }
}
