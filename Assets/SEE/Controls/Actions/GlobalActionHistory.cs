using SEE.Utils;
using static SEE.Utils.ActionHistory;

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
        public static void Push(GlobalHistoryEntry action)
        {
            history.Push(action);
        }

        /// <summary>
        /// Replaces an item in the globalHistory.
        /// </summary>
        /// <param name="oldItem">the item that should be replaced.</param>
        /// <param name="newItem">the new item inserted instead.</param>
        /// <param name="isNetwork">true, if the call comes from another client through the network, else false.</param>
        public static void Replace(GlobalHistoryEntry oldItem, GlobalHistoryEntry newItem, bool isNetwork)
        {
            history.Replace(oldItem, newItem, isNetwork);
        }

        /// <summary>
        /// Deletes an item from the action list depending on its ID.
        /// </summary>
        /// <param name="id">the ID of the action which should be deleted</param>
        public static void DeleteItem(string id)
        {
            history.DeleteItem(id);
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of the currently executed 
        /// action. If no such action is currently executed, null is returned.
        /// </summary>
        /// <returns><see cref="ActionStateType"/> of the currently executed action or null</returns>
        public static ActionStateType Current()
        {
            return history.LastAction?.GetActionStateType();
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
