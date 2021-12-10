using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Propagates the re-parenting of a game node through the network.
    /// </summary>
    internal class ReparentNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject of a game node that needs to be re-parented.
        /// </summary>
        public string ChildObjectID;

        /// <summary>
        /// The unique name of the gameObject of a game node becoming the new parent of <see cref="ChildObjectID"/>.
        /// </summary>
        public string ParentObjectID;

        /// <summary>
        /// Where <see cref="ChildObjectID"/> should be placed in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="childObjectID">the unique game-object name of the child game object to be re-parented</param>
        /// <param name="parentObjectID">the unique game-object name of the game object becoming the parent of <paramref name="childObjectID"/></param>
        /// <param name="position">the new position of the child game object</param>
        public ReparentNetAction(string childObjectID, string parentObjectID, Vector3 position)
        {
            ChildObjectID = childObjectID;
            ParentObjectID = parentObjectID;
            Position = position;
        }

        /// <summary>
        /// Re-parenting in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(ChildObjectID);
                if (gameObject != null)
                {
                    Debug.Log($"[Net] Reparanting {gameObject.name} under {ParentObjectID} at {Position}.\n");
                    GameNodeMover.Reparent(gameObject, ParentObjectID, Position);
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {ChildObjectID}.");
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
