using System;
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
    /// Thrown if a client calls <see cref="ActionHistory.Undo"/> while
    /// there is no action running.
    /// </summary>
    public class NoActionRunningException : System.Exception { }

    /// <summary>
    /// Maintains a history of executed action that can undone and redone.
    /// </summary>
    public class ActionHistory
    {
        /// <summary>
        /// The history of actions that have been executed (and have not yet undone). The currently
        /// executed action is contained in this stack only if it was completed (either by returning
        /// true upon its Update or because another action is executed by way of 
        /// <see cref="Execute(ReversibleAction)"/>, <see cref="Undo"/>, or <see cref="Redo"/>.
        /// </summary>
        private Stack<ReversibleAction> UndoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The history of actions that have been undone.
        /// </summary>
        private Stack<ReversibleAction> RedoStack { get; set; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The currently executed action.
        /// </summary>
        public ReversibleAction Current { get; private set; }

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
            if (Current != null)
            {
                Current.Stop();
                UndoStack.Push(Current);
            }
            Current = action;
            Current.Awake();
            Current.Start();
            // Whenever a new action is excuted, we consider the redo stack lost.
            RedoStack.Clear();
        }
        
        /// <summary>
        /// Calls <see cref="ReversibleAction.Update"/> for the currently executed action of this 
        /// action history if there is any; otherwise nothing happens.
        /// </summary>
        public void Update()
        {
            if (Current != null && Current.Update())
            {
                // We are continuing with a fresh instance of the same type as Current.
                Execute(Current.NewInstance());
            }
            Dump();
        }

        private string previousMessage = "";
        private void Dump()
        {
            string newMessage = $"Current: {ToString(Current)} Undo: {ToString(UndoStack)} Redo: {ToString(RedoStack)}\n";
            if (previousMessage != newMessage)
            {
                previousMessage = newMessage; 
                Debug.Log(previousMessage);
            }            
        }

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

        private object ToString(ReversibleAction current)
        {
            if (current == null)
            {
                return "NULL";
            }
            else
            {
                return current + "@" + current.GetType().Name;
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
            if (Current == null && UndoStack.Count == 0)
            {
                throw new EmptyActionHistoryException();
            }
            else
            {
                // We will cancel the current action, that means not only to stop but also
                // to undo it. 
                if (Current != null)
                {
                    Current.Stop();
                    Current.Undo();
                }

                if (UndoStack.Count == 0)
                {
                    // The current action was canceled, but in the absence of any other
                    // action on the UndoStack it will be able to continue from scratch.
                    Current?.Start();
                }
                else
                {
                    // The current action was canceled and we can continue with the action from
                    // the UndoStack. The current action, however, may needed to be resumed later,
                    // hence we need to push it onto the RedoStack.
                    if (Current != null)
                    {
                        RedoStack.Push(Current);
                    }
                    Current = UndoStack.Pop();
                    Current.Undo();
                    Current.Start();
                }
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
                // cancel the currently executed action
                if (Current != null)
                {
                    Current.Stop();
                    //Current.Redo();
                    UndoStack.Push(Current);
                }
                // the last undone action becomes the currently executed action again
                Current = RedoStack.Pop();
                Current.Redo();
                Current.Start();                
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

