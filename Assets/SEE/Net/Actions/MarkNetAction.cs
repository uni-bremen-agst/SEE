using System;
using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class is responsible for adding a marker via network from one client to all others and
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
        /// The id of the new marker.
        /// </summary>
        public string NewMarkerID;

        /// <summary>
        /// The position of the new marker.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the new marker.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent in which to add the new marker</param>
        /// <param name="newMarkerID">id for the new marker</param>
        /// <param name="position">the position for the new marker</param>
        /// <param name="scale">the scale of the new marker in world space</param>
        public MarkNetAction
        (string parentID,
            string newMarkerID,
            Vector3 position,
            Vector3 scale)
            : base()
        {
            this.ParentID = parentID;
            this.NewMarkerID = newMarkerID;
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
                GameObject parent = GraphElementIDMap.Find(ParentID);
                if (parent == null)
                {
                    throw new Exception($"There is no node with the ID {ParentID}.");
                }

                GameNodeMarker.AddChild(parent, Position, Scale, NewMarkerID);
            }
        }
    }
}