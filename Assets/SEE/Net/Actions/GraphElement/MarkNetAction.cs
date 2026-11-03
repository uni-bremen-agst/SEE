using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    public class MarkNetAction : NodeNetAction
    {
        /// <summary>
        /// Whether the node should be marked on the receiving client.
        /// Public because network actions are serialized.
        /// </summary>
        public bool ShouldMark;

        public MarkNetAction(string gameNodeID, bool shouldMark) : base(gameNodeID)
        {
            ShouldMark = shouldMark;
        }
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);
            if (ShouldMark)
            {
                GameNodeMarker.MarkNode(node);
            }
            else
            {
                GameNodeMarker.UnmarkNode(node);
            }
        }
    }
}

