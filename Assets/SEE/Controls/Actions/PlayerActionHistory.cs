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
        private static GlobalActionHistory history = new GlobalActionHistory();

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
            // if (history.UndoCount > 0)
            //{
                history.Undo("1"); //FIXME: USE THE PLAYERS IP FOR THAT
            //}
        }

        /// <summary>
        /// Re-does the action that was previously undone.
        /// </summary>
        public static void Redo()
        {
            //if (history.RedoCount > 0)
           // {
                history.Redo("1"); //FIXME: USE THE PLAYERS IP FOR THAT
           // }
        }

        /// <summary>
        /// Executes the given kind of <paramref name="actionType"/> as new.
        /// </summary>
        /// <param name="actionType">kind of action to be executed</param>
        public static void Execute(ActionStateType actionType)
        {
            ActionState.Value = actionType;
            history.Execute(actionType.CreateReversible(),"1");
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of the currently executed 
        /// action. If no such action is currently executed, null is returned.
        /// </summary>
        /// <returns><see cref="ActionStateType"/> of the currently executed action or null</returns>
        public static ActionStateType Current()
        {
            return history.GetActiveAction("1")?.GetActionStateType(); //FIXME: USE THE PLAYERS IP FOR THAT
        }

        /// <summary>
        /// True if the action history is empty.
        /// </summary>
        /// <returns>true if the action history is empty</returns>
        public static bool IsEmpty()
        {
            return false;//history.UndoCount == 0;
        }
    }
}
