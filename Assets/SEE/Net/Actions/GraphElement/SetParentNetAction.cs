using SEE.Game;
using SEE.Game.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates <see cref="ReflexionMapper.SetParent"/> or <see cref="GameNodeMover.SetParent"/>,
    /// respectively, through the network.
    /// </summary>
    internal class SetParentNetAction : NodeNetAction
    {
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
        /// <param name="childID">The unique game-object name of the child to
        /// be put and fit onto the <paramref name="newParentID"/>;
        /// must be known to <see cref="GraphElementIDMap"/>.</param>
        /// <param name="newParentID">The unique game-object name of the game object becoming the
        /// new parent of <paramref name="childID"/>;
        /// must be known to <see cref="GraphElementIDMap"/>.</param>
        /// <param name="reflexion">If true, <see cref="ReflexionMapper.SetParent"/> will
        /// be called; otherwise <see cref="GameNodeMover.SetParent"/>.</param>
        public SetParentNetAction(string childID, string newParentID, bool reflexion) : base(childID)
        {
            NewParentID = newParentID;
            Reflexion = reflexion;
        }

        /// <summary>
        /// Setting the new parent in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (Reflexion)
            {
                ReflexionMapper.SetParent(Find(GraphElementID), Find(NewParentID));
            }
            else
            {
                GameNodeMover.SetParent(Find(GraphElementID), Find(NewParentID));
            }
        }
    }
}
