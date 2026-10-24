using UnityEngine;
using SEE.SceneManipulation;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Marks or unmarks a node on all clients.
    /// </summary>
    public class MarkNetAction : NodeNetAction
    {
        // Public fields are serialized for network transfer.
        public bool Marked;
        public Color Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">ID of the node to mark or unmark.</param>
        /// <param name="marked">Whether the node should be marked.</param>
        /// <param name="color">Color of the marker.</param>
        public MarkNetAction(
            string nodeID,
            bool marked,
            Color color)
            : base(nodeID)
        {
            Marked = marked;
            Color = color;
        }

        /// <summary>
        /// Applies the action on another client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject node = Find(GraphElementID);

            if (Marked)
            {
                GameNodeMarker.Mark(node, Color);
            }
            else
            {
                GameNodeMarker.Unmark(node);
            }
        }
    }
}