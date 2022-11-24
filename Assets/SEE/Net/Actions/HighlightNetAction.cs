using SEE.Game;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="Highlighter.SetHighlight"/> through the network.
    /// </summary>
    internal class HighlightNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject whose highlighting is to be set.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// If true, the game object identified by <see cref="GameObjectID"/>
        /// will be highlighted; otherwise its highlighting will be turned off.
        /// </summary>
        public bool Highlight;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the child to
        /// be put and fit onto the <paramref name="newParentID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="highlight">If true, the game object identified by <see cref="GameObjectID"/>
        /// will be highlighted; otherwise its highlighting will be turned off.</param>
        public HighlightNetAction(string gameObjectID, bool highlight)
        {
            GameObjectID = gameObjectID;
            Highlight = highlight;
        }

        /// <summary>
        /// Setting the highlighting in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Highlighter.SetHighlight(Find(GameObjectID), Highlight);
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
