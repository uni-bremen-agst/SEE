using SEE.Game.SceneManipulation;
using SEE.Net.Actions;
using UnityEngine;

namespace SEE.Net
{
    public class MarkNetAction : AbstractNetAction
    {
        /// <summary>
        /// The ID of the parent gameObject of the new GameObject.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The id of the new node.
        /// </summary>
        public string NewNodeID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent in which to add the new node</param>
        /// <param name="newNodeID">id for the new node</param>
        public MarkNetAction
            (string parentID,
             string newNodeID)
            : base()
        {
            ParentID = parentID;
            NewNodeID = newNodeID;
        }
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        //FIXME: Delete the sphere must be implemented too. Maybe with a new class just for deleting
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameNodeMarker.AddMarker(Find(ParentID));
            }
        }
    }
}