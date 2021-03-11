using SEE.Utils;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class manages the history of actions triggered by the player and that
    /// can be undone and re-done.
    /// </summary>
    public static class PlayerActionHistory
    {
        /// <summary>
        /// The history of actions.
        /// </summary>
        private static ActionHistory history = new ActionHistory();

        /// <summary>
        /// Executes the currently active action (if there is any).
        /// Expected to be called once for every frame.
        /// </summary>
        public static void Update()
        {
            history.Update();
        }

        /// <summary>
        /// Undoes the currently active action (if there is any).
        /// </summary>
        public static void Undo()
        {
            if (history.UndoCount > 0)
            {
                history.Undo();
            }
        }

        /// <summary>
        /// Re-does the action that was previously undone.
        /// </summary>
        public static void Redo()
        {
            if (history.RedoCount > 0)
            {
                history.Redo();
            }
        }

        /// <summary>
        /// The specific action state type of each action.
        /// </summary>
        public static ActionStateType Value;

        /// <summary>
        /// Executes the given kind of <paramref name="actionType"/> as new.
        /// </summary>
        /// <param name="actionType">kind of action to be executed</param>
        public static void Execute(ActionStateType actionType)
        {
            if (actionType.CreateReversible != null)
            {
                history.Execute(actionType.CreateReversible());
            }
        }
    }
}
