using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    /// Maintains a history of executed reversible actions that can be undone and redone.
    /// </summary>
    public class ActionHistory
    {
        /// <summary>
        /// The history of actions that have been executed (and have not yet been undone). The currently
        /// executed action is the top element of this stack.
        /// </summary>
        private Stack<ReversibleAction> UndoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The history of actions that have been undone.
        /// </summary>
        private Stack<ReversibleAction> RedoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The currently executed action. May be null if there is no current action running.
        /// </summary>
        public ReversibleAction Current
        {
            get
            {
                if (UndoStack.Count > 0)
                {
                    return UndoStack.Peek();
                }
                else
                {
                    return null;
                }
            }
        }

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
            Current?.Stop();
            UndoStack.Push(action);
            action.Awake();
            action.Start();
            // Whenever a new action is excuted, we consider the redo stack lost.
            RedoStack.Clear();
        }

        /// <summary>
        /// Calls <see cref="ReversibleAction.Update"/> for the currently executed action of this 
        /// action history if there is any. If that action signals that it is complete (via
        /// <see cref="ReversibleAction.Update"/> a new instance of the same kind as this
        /// action will be created, added to the action history and become the new currently
        /// executed action. If there is no currently executed action nothing happens.
        /// </summary>
        public void Update()
        {
            Debug.Log(Current + "current");
            if (Current != null && Current.Update())
            {
                // We are continuing with a fresh instance of the same type as Current.
                Execute(Current.NewInstance());
            }
            // Dump();
        }

        /// <summary>
        /// A memory of the previously emitted debugging output.
        /// </summary>
        private string previousMessage = "";

        /// <summary>
        /// Emits the current UndoStack and RedoStack as debugging output.
        /// If the output would be the same a in the previous call, nothing
        /// is emitted.
        /// </summary>
        private void Dump()
        {
            string newMessage = $"Current: {ToString(Current)} Undo: {ToString(UndoStack)} Redo: {ToString(RedoStack)}\n";
            if (previousMessage != newMessage)
            {
                previousMessage = newMessage;
                Debug.Log(previousMessage);
            }
        }

        /// <summary>
        /// Returns a human readable representation of the given <paramref name="stack"/>.
        /// The top element comes first. Used for debugging.
        /// </summary>
        /// <param name="stack">stack whose content is to be emitted</param>
        /// <returns>human readable representation</returns>
        private object ToString(Stack<ReversibleAction> stack)
        {
            if (stack.Count == 0)
            {
                return "[]";
            }
            else
            {
                StringBuilder sb = new StringBuilder("[");
                foreach (ReversibleAction action in stack)
                {
                    sb.Append(ToString(action));
                    sb.Append(" ");
                }
                sb.Length--; // remove last blank 
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns a human readable representation of the given <paramref name="action"/>.
        /// Used for debugging.
        /// </summary>
        /// <param name="action">action to be emitted</param>
        /// <returns>human readable representation</returns>
        private object ToString(ReversibleAction action)
        {
            if (action == null)
            {
                return "NULL";
            }
            else
            {
                return action + "@" + action.GetType().Name;
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
        /// <exception cref="EmptyActionHistoryException">thrown if this action history is empty and 
        /// no action is currently running</exception>
        public void Undo()
        {
            if (UndoStack.Count == 0)
            {
                throw new EmptyActionHistoryException();
            }
            else
            {
                // The top element of the UndoStack is the current action. It
                // may or may not be completed. The latter is the case when
                // multiple Undos occur in a row. For the very first Undo without
                // prior Undo the action is still running and is not yet completed.
                // In that case, it may have had some preliminary effects already
                // or not (signalled by HadEffect). If it has had preliminary effects,
                // we will treat it similarly to a completed action, that is, undo
                // its effects and push it onto the RedoStack. This way it may 
                // be resumed by way of Redo. If it has had no effect yet, we do not
                // undo it and it will not be pushed onto RedoStack. Instead we
                // will just pop it of the UndoStack and continue with the next action
                // on the UndoStack. The reason for this decision is as follows: It would
                // be confusing for a user if we would handled actions without effect
                // as normal actions because the user would not get any visible 
                // feedback of her/his Undo decision because that kind of action has
                // not had any effect yet.

                ReversibleAction current = UndoStack.Pop();
                while (!current.HadEffect())
                {
                    current.Stop();
                    if (UndoStack.Count > 0)
                    {
                        // continue with next action until we find one that has had an effect
                        current = UndoStack.Pop();
                    }
                    else
                    {
                        // all actions undone
                        return;
                    }
                }

                // assert: current has had an effect
                current.Stop();
                current.Undo();
                RedoStack.Push(current);

                // Now we will resume with the top of the UndoStack.
                // Watch out: Current relates to new the top element of UndoStack while 
                // current relates to a previous top element that was just undone.
                Current?.Start();
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
                Current?.Stop();
                // the last undone action becomes the currently executed action again
                ReversibleAction action = RedoStack.Pop();
                UndoStack.Push(action);
                action.Redo();
                action.Start();
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
