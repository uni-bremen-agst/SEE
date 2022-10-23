using SEE.Game;
using UnityEngine;

/// <summary>
/// Executes the mark node action on remote clients
/// </summary>
namespace SEE.Net.Actions
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
        /// Constructor.
        /// </summary>
        /// <param name="parentID">unique ID of the parent which to highlight</param>
        public MarkNetAction
            (string parentID)
            : base()
        {
            this.ParentID = parentID;
        }

        /// <summary>
        /// Executes the mark node action on remote clients
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
                GameNodeMarker.CreateMarker(parent);
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
    }
}