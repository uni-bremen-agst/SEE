using SEE.Game;
using SEE.Net;
using UnityEngine;

namespace SEE.Net
{
    public class MarkNetAction : AbstractNetAction
    {

        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The ID of the parent gameObject of the marker.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The position of the marker's parent.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the marker.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent which to highlight</param>
        /// <param name="position">the position of the marker's parent</param>
        /// <param name="scale">the scale of the marker in world space</param>
        public MarkNetAction
            (string parentID,
             Vector3 position,
             Vector3 scale)
            : base()
        {
            ParentID = parentID;
            Position = position;
            Scale = scale;
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject parent = GraphElementIDMap.Find(ParentID);
                if (parent == null)
                {
                    throw new System.Exception($"There is no node with the ID {ParentID}.");
                }
                GameNodeMarker.CreateMarker(parent, Position, Scale);
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // intentionally left empty
        }

    }
}