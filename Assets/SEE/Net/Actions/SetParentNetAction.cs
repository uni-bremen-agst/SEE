using SEE.Game;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="ReflexionMapper.SetParent"/> or <see cref="GameNodeMover.SetParent"/>,
    /// respectively, through the network.
    /// </summary>
    internal class SetParentNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the child gameObject that needs to be put onto a new parent.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string ChildID;

        /// <summary>
        /// The unique name of the gameObject that becomes the new parent of the child.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string NewParentID;

        /// <summary>
        /// If true, <see cref="ReflexionMapper.SetParent"/> will be called;
        /// otherwise <see cref="GameNodeMover.SetParent"/>.
        /// </summary>
        public bool Reflexion;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="childID">the unique game-object name of the child to
        /// be put and fit onto the <paramref name="newParentID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="newParentID">the unique game-object name of the game object becoming the
        /// new parent of <paramref name="childID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="reflexion">if true, <see cref="ReflexionMapper.SetParent"/> will
        /// be called; otherwise <see cref="GameNodeMover.SetParent"/></param>
        public SetParentNetAction(string childID, string newParentID, bool reflexion)
        {
            ChildID = childID;
            NewParentID = newParentID;
            Reflexion = reflexion;
        }

        /// <summary>
        /// Setting the new parent in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (Reflexion)
                {
                    ReflexionMapper.SetParent(Find(ChildID), Find(NewParentID));
                }
                else
                {
                    GameNodeMover.SetParent(Find(ChildID), Find(NewParentID));
                }
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
