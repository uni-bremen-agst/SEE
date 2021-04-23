using Assets.SEE.Utils;
using System;
using System.Collections.Generic;
using static Assets.SEE.Utils.ActionHistory;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class manages the history of actions triggered by the player and that
    /// can be undone and re-done.
    /// </summary>
    public static class GlobalActionHistory
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
            history.Undo();
        }

        /// <summary>
        /// Re-does the action that was previously undone.
        /// </summary>
        public static void Redo()
        {
            history.Redo();
        }

        /// <summary>
        /// Executes the given kind of <paramref name="actionType"/> as new.
        /// </summary>
        /// <param name="actionType">kind of action to be executed</param>
        public static void Execute(ActionStateType actionType)
        {
            ActionState.Value = actionType;
            history.Execute(actionType.CreateReversible());
        }

        /// <summary>
        /// Pushes new actions to the <see cref="allActionsList"/>
        /// </summary>
        /// <param name="action">The action and all of its specific values which are needed for the history</param>
        public static void Push(Tuple<bool, HistoryType, string, List<string>> action)
        {
            history.Push(action);
        }

        /// <summary>
        /// Deletes an item from the action list depending on its id.
        /// </summary>
        /// <param name="id">the id of the action which should be deleted</param>
        public static void DeleteItem(string id, bool isOwner)
        {
            history.DeleteItem(id, isOwner);
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of the currently executed 
        /// action. If no such action is currently executed, null is returned.
        /// </summary>
        /// <returns><see cref="ActionStateType"/> of the currently executed action or null</returns>
        public static ActionStateType Current()
        {
            return history.GetActiveAction()?.GetActionStateType();
        }

        /// <summary>
        /// True if the action history is empty.
        /// </summary>
        /// <returns>true if the action history is empty</returns>
        public static bool IsEmpty()
        {
            return history.NoActionsLeft();
        }
    }
}
