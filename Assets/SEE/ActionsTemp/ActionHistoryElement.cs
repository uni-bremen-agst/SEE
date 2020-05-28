using UnityEngine;

public class ActionHistoryElement : MonoBehaviour
{
    public int index;

    public void OnUndo()
    {
        SEE.Controls.ActionHistory.Undo(index);
    }

    public void OnRedo()
    {
        SEE.Controls.ActionHistory.Redo(index);
    }
}
