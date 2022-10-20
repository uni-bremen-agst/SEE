using SEE.Game;
using UnityEngine;

namespace SEE.Net
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
        /// The node which is marked.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The position of the mark in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the new mark in world space.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the Node which will get marked.</param>
        /// <param name="position">the position for the mark.</param>
        /// <param name="scale">the scale of the mark in world space.</param>
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
        /// Toggle the marking of the node on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject parent = GraphElementIDMap.Find(ParentID);
                if (parent == null)
                {
                    throw new System.Exception($"There is no node with the ID {ParentID}.");
                }
                GameNodeMarker.Mark(parent, Position, Scale);
            }
        }
    }
}
