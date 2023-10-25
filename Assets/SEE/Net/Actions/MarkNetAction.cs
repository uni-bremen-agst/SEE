using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for marking a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The ID of the parent gameObject of the new GameObject.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The position of the new sphere.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the new sphere.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent on which to add the new sphere</param>
        /// <param name="position">the position for the new sphere</param>
        /// <param name="scale">the scale of the new sphere in world space</param>
        public MarkNetAction
            (string parentID,
            Vector3 position,
            Vector3 scale)
            : base()
        {
            this.ParentID = parentID;
            this.Position = position;
            this.Scale = scale;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates a new GameObject on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameNodeMarker.NewSphere(Find(ParentID), Scale);
            }
        }
    }
}
