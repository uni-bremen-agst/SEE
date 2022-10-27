using System;
using SEE.Game;
using UnityEngine;


namespace SEE.Net.Actions
{
    /// <summary>
    /// Author: Hannes Kuss
    /// 
    /// Action responsible for marking nodes on all other users in the session.
    /// It uses <see cref="GameNodeMarker"/> to create and delete the marked spheres of the code city nodes.
    /// The class is used mainly by <see cref="SEE.Controls.Actions.MarkAction"/>.
    ///
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the node which was changed.
        /// With this id the code city node that was marked can then be find on the client's side and their node
        /// can be marked too.
        /// </summary>
        public string NodeId;

        /// <summary>
        /// Added is true, when the node <see cref="Node"/> was marked and is false if the node was unmarked
        /// </summary>
        public bool Added;

        /// <summary>
        /// Default constructor of the <see cref="MarkNetAction"/>.
        /// </summary>
        /// <param name="nodeid">The node which the user interacted with</param>
        /// <param name="added">True if the clicked node was marked and false then the clicked node was unmarked</param>
        public MarkNetAction(string nodeid, bool added)
        {
            NodeId = nodeid;
            Added = added;
        }

        protected override void ExecuteOnServer()
        {
            // should be blank
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject node = GraphElementIDMap.Find(NodeId) ??
                                  throw new SystemException($"node {NodeId} not found");

                if (Added)
                {
                    GameNodeMarker.CreateMarker(node);
                }
                else
                {
                    GameNodeMarker.RemoveMarker(node);
                }
            }
        }
    }
}