using System.Collections.Generic;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class records a history of holistic metric actions so we can revert them.
    /// </summary>
    internal static class HolisticMetricsActionHistory
    {
        /// <summary>
        /// Points to the action that will be undone next if the player clicks undo in the game. If there is nothing
        /// to undo, it is set to -1.
        /// </summary>
        private static int lastAction = -1;

        /// <summary>
        /// The history of actions, where the one at index 0 is the oldest and the one at the highest index is the
        /// newest.
        /// </summary>
        private static readonly List<Action> actionHistory = new List<Action>();

        /// <summary>
        /// Adds a new action to the history after the action that the history currently points to. If any actions were
        /// reverted before (meaning they have a higher index than what the action currently pointed to), those will be
        /// forgotten.
        /// </summary>
        /// <param name="action">The action to add to the history</param>
        internal static void Add(Action action)
        {
            lastAction++;  // Position at which we will add the new action.

            // Calculate if there is anything to delete at the end.
            int numberToDelete = actionHistory.Count - lastAction;

            // If there is anything to delete, delete it
            if (numberToDelete > 0)
            {
                actionHistory.RemoveRange(lastAction, numberToDelete);
            }
            
            // Append the new action at the end of the list.
            actionHistory.Add(action);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static void Undo()
        {
            if (lastAction >= 0)
            {
                actionHistory[lastAction].Undo();
                lastAction--;
            }
        }

        /// <summary>
        /// Redoes the action last undone.
        /// </summary>
        internal static void Redo()
        {
            if (lastAction + 1 < actionHistory.Count)  // Check if there is anything to redo.
            {
                actionHistory[++lastAction].Do();
            }
        }
    }
}