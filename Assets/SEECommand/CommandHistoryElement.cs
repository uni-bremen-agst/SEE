using UnityEngine;

public class CommandHistoryElement : MonoBehaviour
{
    public int index;

    public void OnUndo()
    {
        SEE.Command.CommandHistory.Undo(index);
    }

    public void OnRedo()
    {
        SEE.Command.CommandHistory.Redo(index);
    }
}
