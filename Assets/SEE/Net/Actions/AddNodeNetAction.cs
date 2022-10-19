using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for adding a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class AddNodeNetAction : AbstractNetAction
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
        public AddNodeNetAction
            (string parentID,
             string newNodeID,
             Vector3 position,
             Vector3 scale)
            : base()
        {
            this.ParentID = parentID;
            this.NewNodeID = newNodeID;
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
                    throw new System.Exception($"There is no node with the ID {ParentID}.");
                }
                GameNodeAdder.AddChild(parent, Position, Scale, NewNodeID);
            }
        }
    }
}
