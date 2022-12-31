using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the version change of the graph through the network.
    /// </summary>
    internal class VersionNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject representing a node of the graph whose version was changed.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the game object representing a node of the
        /// graph whose version was changed. Must be known to <see cref="GraphElementIDMap"/></param>
        public VersionNetAction(string gameObjectID)
        {
            GameObjectID = gameObjectID;
        }

        /// <summary>
        /// Version change in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameNodeMover.NewMovementVersion(Find(GameObjectID));
            }
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
