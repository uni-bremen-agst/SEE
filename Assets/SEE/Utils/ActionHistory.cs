using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Thrown if a client calls <see cref="ActionHistory.Undo"/> for an empty
    /// action history.
    /// </summary>
    public class EmptyActionHistoryException : System.Exception { }

    /// <summary>
    /// Thrown if a client calls <see cref="ActionHistory.Redo"/> for an 
    /// action history that has an empty list of undone actions.
    /// </summary>
    public class EmptyUndoHistoryException : System.Exception { }

    /// <summary>
    /// Maintains a history of executed action that can undone and redone.
    /// </summary>
    public class ActionHistory
    {
        /// <summary>
        /// The history of actions that have been executed (and have not yet undone). The currently
        /// executed action is the top element in this stack.
        /// </summary>
        public Stack<ReversibleAction> UndoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The history of actions that have been undone.
        /// </summary>
        private Stack<ReversibleAction> RedoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// Let C be the currently executed action (if there is any) in this action history. 
        /// Then <see cref="ReversibleAction.Stop"/> will be called for C. After that 
        /// <see cref="ReversibleAction.Awake()"/> and then <see cref="ReversibleAction.Start"/>
        /// will be called for <paramref name="action"/> and <paramref name="action"/> is added to 
        /// the action history and becomes the currently executed action for which 
        /// <see cref="ReversibleAction.Update"/> will be called whenever a client
        /// of this action history calls the action history's <see cref="Update"/> method.
        /// 
        /// No action previously undone can be redone anymore.
        /// 
        /// Precondition: <paramref name="action"/> is not already present in the action history.
        /// </summary>
        /// <param name="action">the action to be executed</param>
        public void Execute(ReversibleAction action)
        {
            if (UndoStack.Count > 0)
            {
                UndoStack.Peek().Stop();
            }
            UndoStack.Push(action);
            action.Awake();
            action.Start();
            // Whenever a new action is excuted, we consider the redo stack lost.
            RedoStack.Clear();
        }

        /// <summary>
        /// Calls <see cref="ReversibleAction.Update"/> for the currently executed action of this 
        /// action history if there is any; otherwise nothing happens.
        /// </summary>
        public void Update()
        {
            if (UndoStack.Count > 0)
            {
                UndoStack.Peek().Update();
            }
        }

        /// <summary>
        /// Let C be the currently executed action in this action history. 
        /// First <see cref="ReversibleAction.Stop"/> and then <see cref="ReversibleAction.Undo"/> 
        /// will be called for C and C is removed from the action history (yet preserved for a 
        /// possible <see cref="Redo"/> potentially being requested later).
        /// Let C' be the action that was executed just before C, that is, was added by <see cref="Execute"/>
        /// just before C (if there is any). C' becomes the currently executed action (thus receiving
        /// an <see cref="ReversibleAction.Update"/> whenever a client of this action history calls 
        /// <see cref="Update"/>) and <see cref="ReversibleAction.Start"/> is called for C'.
        /// 
        /// Precondition: There must be a currently executing action, that is, this action
        /// history must not be empty.
        /// </summary>
        /// <exception cref="EmptyActionHistoryException">thrown if this action history is empty</exception>
        public void Undo()
        {
            if (UndoStack.Count > 0)
            {
                // If the user "undoes" an action, we pop off the undo stack (let the popped off 
                // action be A), call the Undo operation of A, and then we push A onto the redo stack.
                ReversibleAction currentAction = UndoStack.Pop();
                currentAction.Stop();
                currentAction.Undo();                   
                RedoStack.Push(currentAction);

                if (UndoStack.Count > 0)
                {
                    UndoStack.Peek().Start();
                }
            }
            else
            {
                throw new EmptyActionHistoryException();
            }
        }

        /// <summary>
        /// Let C be the currently executed action and U be the last undone action in this action history. 
        /// First <see cref="ReversibleAction.Stop"/> will be called for C. Then U will be removed from
        /// the list of undone actions and <see cref="ReversibleAction.Redo"/> and then 
        /// <see cref="ReversibleAction.Start"/> will be called for U. U becomes the currently executed
        /// action (thus receiving an <see cref="ReversibleAction.Update"/> whenever a client of this 
        /// action history calls <see cref="Update"/>).
        /// 
        /// Precondition: There must be at least one action that was undone (and not again redone).
        /// </summary>
        /// <exception cref="EmptyUndoHistoryException">thrown if there is no action previously undone</exception>
        public void Redo()
        {
            if (RedoStack.Count > 0)
            {
                // stop the currently executed action
                if (UndoStack.Count > 0)
                {
                    UndoStack.Peek().Stop();
                }
                // the last undone action becomes the currently executed action again
                ReversibleAction redoAction = RedoStack.Pop();
                UndoStack.Push(redoAction);
                redoAction.Redo();
                redoAction.Start();                
            }
            else
            {
                throw new EmptyUndoHistoryException();
            }
        }

        /// <summary>
        /// The number of executed actions that can be undone.
        /// </summary>
        /// <returns>maximal number of actions that can be undone</returns>
        public int UndoCount
        {
            get => UndoStack.Count;
        }

        /// <summary>
        /// The number of undone actions that can be re-done.
        /// </summary>
        /// <returns>maximal number of actions that can be re-done</returns>
        public int RedoCount
        {
            get => RedoStack.Count;
        }
    }
}

