using System;
using SEE.Game;
using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="GameNodeMover.PlaceOn"/> through the network.
    /// </summary>
    internal class PlaceOnNetAction : AbstractNetAction
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
        public PlaceOnNetAction(string childID, string newParentID)
        {
            ChildID = childID;
            NewParentID = newParentID;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject child = Find(ChildID);
            GameObject newParent = Find(NewParentID);
            GameNodeMover.PlaceOn(child.transform, newParent);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
