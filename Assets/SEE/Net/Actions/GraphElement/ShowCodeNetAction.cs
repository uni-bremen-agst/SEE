using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Network action that propagates the "show code" interaction
    /// of a game object to other clients.
    /// </summary>
    internal class ShowCodeNetAction : GraphElementNetAction
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">
        /// The unique identifier of the game object whose code should be shown;
        /// must be known to <see cref="GraphElementIDMap"/>.
        /// </param>
        public ShowCodeNetAction(string gameObjectID) : base(gameObjectID) { }

        /// <summary>
        /// Executes the action on a client by opening the code window
        /// for the specified game object.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameWindowManager.ActivateWindow(ShowCodeAction.ShowCode(Find(GraphElementID).MustGetComponent<GraphElementRef>()));
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
