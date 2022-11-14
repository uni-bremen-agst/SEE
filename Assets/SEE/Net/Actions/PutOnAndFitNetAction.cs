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
        /// The unique name of the gameObject that becomes the new parent of the child.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string OriginalParentID;

        /// <summary>
        /// The original scale of the child relative to its original parent (local scale).
        ///
        /// </summary>
        public Vector3 OriginalLocalScale;


        /// <param name="child">game object to be put onto <paramref name="newParent"/></param>
        /// <param name="newParent">where to put <paramref name="child"/></param>
        /// <param name="originalParent">the original <paramref name="originalParent"/> of <paramref name="child"/></param>
        /// <param name="originalLocalScale">original local scale of <paramref name="child"/> relative to
        /// <paramref name="originalParent"/>; used to restore this scale if <paramref name="newParent"/>
        /// and <paramref name="originalParent"/> are the same</param>
        ///
        /// <summary>
        /// The duration of the movement animation in seconds.
        /// </summary>
        public float Duration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="childID">the unique game-object name of the game object to be moved;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        public PutOnAndFitNetAction(string childID, Vector3 originalLocalScale, float animationDuration)
        {
            ChildID = childID;
            OriginalLocalScale = originalLocalScale;
            Duration = animationDuration;
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
