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
    /// Both: properties are readonly because the clients don't need to alter any of the values.
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        /// <summary>
        /// The node which was changed
        /// </summary>
        public GameObject Node { get; }

        /// <summary>
        /// Added is true, when the node <see cref="Node"/> was marked and is false if the node was unmarked
        /// </summary>
        public bool Added { get; }

        /// <summary>
        /// Default and only constructor of the <see cref="MarkNetAction"/>.
        /// </summary>
        /// <param name="node">The node which the user interacted with</param>
        /// <param name="added">True if the clicked node was marked and false then the clicked node was unmarked</param>
        public MarkNetAction(GameObject node, bool added)
        {
            Node = node;
            Added = added;
        }

        protected override void ExecuteOnServer()
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteOnClient()
        {
            if (Added)
            {
                GameNodeMarker.CreateMarker(Node);
            }
            else
            {
                GameNodeMarker.RemoveMarker(Node);
            }
        }
    }
}