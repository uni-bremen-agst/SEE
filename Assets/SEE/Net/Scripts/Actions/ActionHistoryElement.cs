using UnityEngine;

namespace SEE.Net
{

    public class ActionHistoryElement : MonoBehaviour
    {
        public int index;

        public void OnUndo()
        {
            ActionHistory.Undo(index);
        }

        public void OnRedo()
        {
            ActionHistory.Redo(index);
        }
    }

}
