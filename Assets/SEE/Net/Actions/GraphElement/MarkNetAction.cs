using SEE.SceneManipulation;
using System;
using System.Collections.Generic;
using System.Text;


namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    ///broadcast an action of the local player to all clients, that is, to replicate
    ///the action in all client code cities such that all players always have a consistent view of the SEE scenery
    /// </summary>
    internal class MarkNetAction : NodeNetAction
    {
        public MarkNetAction(string gameNodeID) : base(gameNodeID)
        {

        }

        /// <summary>
        /// Toggles the marking of the game node identified by <see cref="GraphElementNetAction.GraphElementID"/>
        /// on this client, that is, adds a sphere above the node if it is not yet marked
        /// or removes the sphere otherwise.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.ToggleMark(Find(GraphElementID));
        }
    }
}
