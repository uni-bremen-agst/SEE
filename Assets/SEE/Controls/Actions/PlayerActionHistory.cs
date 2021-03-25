using SEE.Utils;
using System.Collections.Generic;

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
        /// Executes the given kind of <paramref name="actionType"/> as new.
        /// </summary>
        /// <param name="actionType">kind of action to be executed</param>
        public static void Execute(ActionStateType actionType)
        {
            history.Execute(actionType.CreateReversible());

            // FIXME: This looks like a hack and should be removed later.
            // The PlayerActionHistory should not need to know anything about its actions.
            if (actionType.Equals(ActionStateType.NewNode) || actionType.Equals(ActionStateType.EditNode))
            {
                NodeInteractionButtons.addOrEditNode = history.Current;
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of the currently executed 
        /// action. If no such action is currently executed, null is returned.
        /// </summary>
        /// <returns><see cref="ActionStateType"/> of the currently executed action or null</returns>
        public static ActionStateType Current()
        {
            return history.Current?.GetActionStateType();
        }

        public static Stack<ReversibleAction> GetUndoHistory()
        {
            return history.UndoStack;
        }

        public static Stack<ReversibleAction> GetRedoHistory()
        {
            return history.RedoStack;
        }
    }
}
