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
        /// Duration of the highlight animation in seconds.
        /// </summary>
        public float Duration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">The unique identifier of the game object to highlight;
        /// must be known to <see cref="GraphElementIDMap"/>.</param>
        /// <param name="duration">The amount of time in seconds the element should be highlighted.</param>
        public ShowInCityNetAction(string gameObjectID, float duration) : base(gameObjectID)
        {
            Duration = duration;
        }

        /// <summary>
        /// Executes the action on a client by highlighting the target game object.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(GraphElementID).Operator().Highlight(Duration);
        }
    }
}
