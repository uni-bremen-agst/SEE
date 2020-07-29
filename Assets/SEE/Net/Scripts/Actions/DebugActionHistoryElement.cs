using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// A buffered action history element with a unique index.
    /// </summary>
    public class DebugActionHistoryElement : MonoBehaviour
    {
        /// <summary>
        /// The unique index of the action. The index can be used to determine execution
        /// order of all actions.
        /// </summary>
        public int index;

        /// <summary>
        /// Undos the action.
        /// </summary>
        public void OnUndo()
        {
            ActionHistory.Undo(index);
        }

        /// <summary>
        /// Redos the action.
        /// </summary>
        public void OnRedo()
        {
            ActionHistory.Redo(index);
        }
    }

}
