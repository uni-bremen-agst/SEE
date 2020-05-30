using UnityEngine;

public class ActionHistoryElement : MonoBehaviour
{
    public int index;

    public void OnUndo()
    {
        SEE.Net.ActionHistory.Undo(index);
    }

    public void OnRedo()
    {
        SEE.Net.ActionHistory.Redo(index);
    }
}
