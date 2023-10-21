using SEE.Game.Scenemanipulation;
using System.Collections;
using System.Collections.Generic;
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
        /// The Parent gameObject.
        /// </summary>
        public GameObject Parent;

        /// <summary>
        /// The position of the node.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the node.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">parent in which to mark the new node</param>
        /// <param name="position">the position for the parent node</param>
        /// <param name="scale">the scale of the parent node in world space</param>
        public MarkNetAction
            (GameObject parent,
             Vector3 position,
             Vector3 scale)
            : base()
        {
            this.Parent = parent;
            this.Position = position;
            this.Scale = scale;
        }

        /// <summary>
        /// Marks a new GameObject on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject markSphere = GameNodeMarker.CreateOrDeleteSphere(Parent, Scale);
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}

