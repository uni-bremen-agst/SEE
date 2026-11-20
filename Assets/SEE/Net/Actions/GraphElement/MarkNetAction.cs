using SEE.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates marking or unmarking a node from one client to all others.
    /// </summary>
    /// <remarks>
    /// The net action transfers the target state (<see cref="Mark"/>) instead of
    /// "toggle". That way, it is idempotent: executing it twice, or on a client
    /// that already has that state, does no harm. This matters for undo/redo and
    /// for clients that join later.
    /// </remarks>
    public class MarkNetAction : NodeNetAction
    {
        // Note: All attributes are public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// True if the node is to be marked, false if its marker is to be removed.
        /// </summary>
        public bool Mark;

        /// <summary>
        /// The color of the marker.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">The ID of the game node to be (un)marked.</param>
        /// <param name="mark">True for marking, false for unmarking.</param>
        /// <param name="color">The color of the marker.</param>
        public MarkNetAction(string nodeID, bool mark, Color color)
            : base(nodeID)
        {
            Mark = mark;
            Color = color;
        }

        /// <summary>
        /// Marks or unmarks the node on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMarker.SetMarked(Find(GraphElementID), Mark, Color);
        }
    }
}
