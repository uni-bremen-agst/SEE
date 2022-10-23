using SEE.Controls.Actions;
using SEE.Game;
using UnityEngine;





namespace SEE.Net.Actions
{


	public class MarkNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The ID of the parent gameObject of the new GameObject.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The id of the new node.
        /// </summary>
        public string NewNodeID;

        /// <summary>
        /// The position of the new node.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the new node.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent in which to add the new node</param>
        /// <param name="newNodeID">id for the new node</param>
        /// <param name="position">the position for the new node</param>
        /// <param name="scale">the scale of the new node in world space</param>
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


        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
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

                GameNodeMarker.addSphere(parent, Position, Scale);
            }
        }



    }

}