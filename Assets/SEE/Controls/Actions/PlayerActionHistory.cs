using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for saving the deleted objects in a history for the possibility of an undo operation.
    /// </summary>
    public class PlayerActionHistory : MonoBehaviour
    {
        public Stack<AbstractPlayerAction> HistoryStack { get; set; } = new Stack<AbstractPlayerAction>();

        public Stack<AbstractPlayerAction> UndoStack { get; set; } = new Stack<AbstractPlayerAction>();

        public Stack<AbstractPlayerAction> RedoStack { get; set; } = new Stack<AbstractPlayerAction>();

        public bool AnotherOperation { get; set; } = false;

        public void Update()
        {
            if (HistoryStack.Count != 0)
            {
                if (HistoryStack.Peek().CurrentState.Equals(AbstractPlayerAction.CurrentActionState.Running))
                {
                    HistoryStack.Peek().Update();
                }
                if (HistoryStack.Peek().CurrentState.Equals(AbstractPlayerAction.CurrentActionState.Executed))
                {
                    UndoStack.Push(HistoryStack.Peek());
                    HistoryStack.Push(HistoryStack.Peek().CreateNew());
                    HistoryStack.Peek().Start();
                    RedoStack.Clear();
                }
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    Redo();
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
        }

        public void Start()
        {

        }

        /// <summary>
        /// Calls the undo of the last executed PlayerAction
        /// </summary>
        public void Undo()
        {
            if (UndoStack.Count >= 1)
            {
                AbstractPlayerAction undoneAction = UndoStack.Peek();
                UndoStack.Pop().Undo();
                RedoStack.Push(undoneAction);
                Debug.Log("RedoStackSize: " + RedoStack.Count);
            }
            else
            {
                Debug.LogError("UndoStack is empty\n");
            }
        }

        /// <summary>
        /// Calls the redo of the last executed PlayerAction
        /// </summary>
        public void Redo()
        {
            if (RedoStack.Count != 0)
            {
                AbstractPlayerAction redoneAction = RedoStack.Peek();
                RedoStack.Pop().Redo();
                UndoStack.Push(redoneAction);
                Debug.Log("UndoStackSize: " + UndoStack.Count);
            }
            else
            {
                Debug.LogError("RedoStack is empty\n");
            }
        }

    }
}
