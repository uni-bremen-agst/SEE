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
        private static GlobalActionHistory history = new GlobalActionHistory(20);

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
            history.Execute(actionType.CreateReversible(),"1"); //FIXME: USE THE PLAYERS IP FOR THAT

            // FIXME: This looks like a hack and should be removed later.
            // The PlayerActionHistory should not need to know anything about its actions.
            //if (actionType.Equals(ActionStateType.NewNode) || actionType.Equals(ActionStateType.EditNode))
            //{
           //     NodeInteractionButtons.addOrEditNode = history.Current;
          //  }
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
        internal static bool IsEmpty() //FIXME WITH CORTRECT RETURN
        {
            return false;//history.UndoCount == 0;
        }
    }
}
