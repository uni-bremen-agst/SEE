using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for adding a node via network from one client to all others and
    /// to the server.
    /// </summary>
    public class AddNodeNetAction : GraphElementNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The ID of the parent gameObject of the new GameObject.
        /// </summary>
        public string ParentID;

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
        /// <param name="parentID">Unique ID of the parent in which to add the new node.</param>
        /// <param name="newNodeID">ID for the new node.</param>
        /// <param name="position">The position for the new node.</param>
        /// <param name="scale">The scale of the new node in world space.</param>
        public AddNodeNetAction
            (string parentID,
             string newNodeID,
             Vector3 position,
             Vector3 scale)
            : base(newNodeID)
        {
            ParentID = parentID;
            Position = position;
            Scale = scale;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates a new GameObject on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeAdder.AddChild(Find(ParentID), Position, Scale, SourceGameNodeId);
        }
    }
}
