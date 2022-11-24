using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="GameNodeMover.PutOnAndFit"/> through the network.
    /// </summary>
    internal class PutOnAndFitNetAction : AbstractNetAction
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
        /// The unique name of the gameObject that was the original parent of the child.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string OriginalParentID;

        /// <summary>
        /// The original scale of the child relative to its original parent (local scale).
        /// </summary>
        public Vector3 OriginalLocalScale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="childID">the unique game-object name of the game object of the child to
        /// be put and fit onto the <paramref name="newParentID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="newParentID">the unique game-object name of the game object becoming the
        /// new parent of <paramref name="childID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="originalParentID">the unique name of the gameObject that was the original
        /// parent of the child;
        /// must be known to <see cref="GraphElementIDMap"/>.</param>
        /// <param name="originalLocalScale">the original local scale of <paramref name="childID"/>
        /// relative to <paramref name="originalParentID"/></param>
        public PutOnAndFitNetAction(string childID, string newParentID, string originalParentID, Vector3 originalLocalScale)
        {
            ChildID = childID;
            NewParentID = newParentID;
            OriginalParentID = originalParentID;
            OriginalLocalScale = originalLocalScale;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject child = Find(ChildID);
                GameObject newParent = Find(NewParentID);
                GameObject originalParent = Find(OriginalParentID);
                GameNodeMover.PutOnAndFit(child.transform, newParent, originalParent, OriginalLocalScale);
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
