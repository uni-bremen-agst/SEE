using SEE.Game;
using SEE.GO;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the "show in city" (highlight) interaction of a game node through the network.
    /// </summary>
    internal class ShowInCityNetAction : GraphElementNetAction
    {
        /// <summary>
        /// Factor applied to the highlight animation duration.
        /// </summary>
        public float DurationFactor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">
        /// The unique identifier of the game object to highlight;
        /// must be known to <see cref="GraphElementIDMap"/>.
        /// </param>
        /// <param name="durationFactor">
        /// Factor applied to the highlight animation duration.
        /// </param>
        public ShowInCityNetAction(string gameObjectID, float durationFactor) : base(gameObjectID)
        {
            DurationFactor = durationFactor;
        }

        /// <summary>
        /// Executes the action on a client by highlighting the target game object.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(GraphElementID).Operator().Highlight(DurationFactor);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
