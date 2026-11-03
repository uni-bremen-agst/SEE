using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Network action that synchronizes the marking state
    /// of a game node with the other clients.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// The state that the node should have on the receiving client.
        ///
        /// This field is public so that it can be serialized
        /// by the network action system
        /// </summary>
        public bool ShouldBeMarked;

        /// <summary>
        /// Creates a new network action for a game node
        /// </summary>
        /// <param name="gameNodeID">ID of the affected node.</param>
        /// <param name="shouldBeMarked">
        /// True if the node should be marked false otherwise
        /// </param>
        public MarkNetAction(string gameNodeID, bool shouldBeMarked)
            : base(gameNodeID)
        {
            ShouldBeMarked = shouldBeMarked;
        }

        /// <summary>
        /// Executes the requested marking state 
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject gameNode = Find(GraphElementID);

            if (gameNode != null)
            {
                GameNodeMarker.ApplyMark(gameNode, ShouldBeMarked);
            }
        }
    }
}        