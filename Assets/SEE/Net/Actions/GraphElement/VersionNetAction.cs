using SEE.Game;
using SEE.Game.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the version change of the graph through the network.
    /// </summary>
    internal class VersionNetAction : GraphElementNetAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">The unique game-object name of the game object representing a node of the
        /// graph whose version was changed. Must be known to <see cref="GraphElementIDMap"/>.</param>
        public VersionNetAction(string gameObjectID) : base(gameObjectID)
        {
        }

        /// <summary>
        /// Version change in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMover.NewMovementVersion(Find(SourceGameNodeId));
        }
    }
}
