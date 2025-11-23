using SEE.Game;
using SEE.Game.SceneManipulation;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="ReflexionMapper.SetParent"/> or <see cref="GameNodeMover.SetParent"/>,
    /// respectively, through the network.
    /// </summary>
    internal class SetParentNetAction : ConcurrentNetAction
    {

        /// <summary>
        /// The unique name of the gameObject that is the old parent of the child.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string OldParentID;

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
        public SetParentNetAction(string childID, string newParentID, string oldParentID, bool reflexion) : base(childID)
        {
            NewParentID = newParentID;
            OldParentID = oldParentID;
            Reflexion = reflexion;
            UseObjectVersion(GameObjectID);
        }

        /// <summary>
        /// Setting the new parent in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (Reflexion)
            {
                ReflexionMapper.SetParent(Find(GameObjectID), Find(NewParentID));
            }
            else
            {
                GameNodeMover.SetParent(Find(GameObjectID), Find(NewParentID));
            }
            SetVersion();
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Undoes the MoveAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            if (Reflexion)
            {
                ReflexionMapper.SetParent(Find(GameObjectID), Find(OldParentID));
            }
            else
            {
                GameNodeMover.SetParent(Find(GameObjectID), Find(OldParentID));
            }
            RollbackNotification();
        }
    }
}
