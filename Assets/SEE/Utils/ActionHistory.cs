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
        private Stack<ReversibleAction> UndoStack { get; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The history of actions that have been undone.
        /// </summary>
        private Stack<ReversibleAction> RedoStack { get; } = new Stack<ReversibleAction>();

        /// <summary>
        /// The currently executed action. May be null if there is no current action running.
        /// </summary>
        public ReversibleAction Current => UndoStack.Count > 0 ? UndoStack.Peek() : null;

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
        /// <see cref="ReversibleAction.Update"/>), a new instance of the same kind as this
        /// action will be created, added to the action history and become the new currently
        /// executed action. If there is no currently executed action, nothing happens.
        /// </summary>
        public void Update()
        {
            if (Current != null && Current.Update())
            {
                // We are continuing with a fresh instance of the same type as Current.
                Execute(Current.NewInstance());                
            }
        }

        /// <summary>
        /// Let C be the currently executed action in this action history. 
        /// First <see cref="ReversibleAction.Stop"/> and then <see cref="ReversibleAction.Undo"/> 
        /// will be called for C and C is removed from the action history (yet preserved for a 
        /// possible <see cref="Redo"/> potentially being requested later).
        /// Let C' be the action that was executed just before C having had an effect 
        /// (preliminary or complete), that is, was added by <see cref="Execute"/>
        /// before C (if there is any). If the progress state of C' is 
        /// <see cref="ReversibleAction.Progress.InProgress"/>,
        /// C' becomes the currently executed action and thus first receives 
        /// <see cref="ReversibleAction.Start"/> message and then  
        /// <see cref="ReversibleAction.Update"/> whenever a client of this action history calls 
        /// <see cref="Update"/>) and <see cref="ReversibleAction.Start"/> is called for C'.
        /// If C' has progress state <see cref="ReversibleAction.Progress.Completed"/>,
        /// a new instance of the same type as C' becomes the currently executed action.
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
                // or not. If it has had preliminary effects,
                // we will treat it similarly to a completed action, that is, undo
                // its effects and push it onto the RedoStack. This way it may 
                // be resumed by way of Redo. If it has had no effect yet, we do not
                // undo it and it will not be pushed onto RedoStack. Instead we
                // will just pop it off the UndoStack and continue with the next action
                // on the UndoStack. The reason for this decision is as follows: It would
                // be confusing for a user if we would undo actions without any effect
                // as normal actions because the user would not get any visible 
                // feedback of her/his Undo decision because that kind of action has
                // not had any effect yet.

                ReversibleAction current = UndoStack.Pop();
                current.Stop();
                LastActionWithEffect(ref current);

                if (current != null)
                {
                    // assert: current has had an effect
                    current.Undo();
                    RedoStack.Push(current);

                    // Now we will resume with the action at the top of the current UndoStack.
                    current = Current;
                    if (current != null)
                    {
                        Resume(current);
                    }
                }
            }
        }

        /// <summary>
        /// Resumes the execution with a fresh instance of the given <paramref name="action"/> 
        /// if its current progress is <see cref="ReversibleAction.Progress.Completed"/>
        /// or otherwise with <paramref name="action"/> if the current progress
        /// is <see cref="ReversibleAction.Progress.InProgress"/>.
        /// </summary>
        /// <param name="action">action to be resumed</param>
        private void Resume(ReversibleAction action)
        {
            if (action.CurrentProgress() == ReversibleAction.Progress.Completed)
            {
                // We will resume with a fresh instance of the current action as
                // the (now) current has already been completed.
                action = action.NewInstance();
                UndoStack.Push(action);
                action.Awake();
            }
            action.Start();
        }

        /// <summary>
        /// Sets given <paramref name="action"/> to the action in <see cref="UndoStack"/>
        /// that has had an effect, i.e., whose current progress is different from
        /// <see cref="ReversibleAction.Progress.NoEffect"/>. All actions on the
        /// <see cref="UndoStack"/> with state <see cref="ReversibleAction.Progress.NoEffect"/>
        /// will be popped off. The resulting <paramref name="action"/> may be null
        /// if none of the actions on the <see cref="UndoStack"/> has had any effect.
        /// </summary>
        /// <param name="action">the last action on the <see cref="UndoStack"/> that
        /// has had any effect (preliminary or complete) or null</param>
        private void LastActionWithEffect(ref ReversibleAction action)
        {
            while (action.CurrentProgress() == ReversibleAction.Progress.NoEffect)
            {
                if (UndoStack.Count > 0)
                {
                    // continue with next action until we find one that has had an effect
                    action = UndoStack.Pop();
                }
                else
                {
                    // all actions undone
                    action = null;
                    return;
                }
            }
        }

        /// <summary>
        /// Let C be the currently executed action and U be the last undone action in this action history. 
        /// First <see cref="ReversibleAction.Stop"/> will be called for C. Then U will be removed from
        /// <see cref="RedoStack"/> and pushed onto <see cref="UndoStack"/> and redone by calling 
        /// <see cref="ReversibleAction.Redo"/> for it. Then the execution resumes with U if 
        /// U has state <see cref="ReversibleAction.Progress.InProgress"/> or with a fresh instance
        /// of the same type as U if it has <see cref="ReversibleAction.Progress.Completed"/>.
        /// Resuming means to intiate the necessary life cycle calls <see cref="ReversibleAction.Awake"/> 
        /// (if a fresh instance was created) and <see cref="ReversibleAction.Start"/>.
        /// <see cref="ReversibleAction.Start"/> will be called for U. U becomes the currently executed
        /// Precondition: There must be at least one action that was undone (and not again redone).
        /// </summary>
        /// <exception cref="EmptyUndoHistoryException">thrown if there is no action previously undone</exception>
        public void Redo()
        {
            if (RedoStack.Count > 0)
            {
                //Dump("Redo ");
                Current?.Stop();
                // The last undone action becomes the currently executed action again.
                // This action may have state <see cref="ReversibleAction.Progress.InProgress"/>
                // or <see cref="ReversibleAction.Progress.Completed"/>.
                ReversibleAction action = RedoStack.Pop();
                UndoStack.Push(action);
                action.Redo();
                Resume(action);
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

        //---------------------------
        // For debugging
        //---------------------------

        /// <summary>
        /// A memory of the previously emitted debugging output.
        /// </summary>
        private string previousMessage = "";

        /// <summary>
        /// Emits the current UndoStack and RedoStack as debugging output.
        /// If the output would be the same a in the previous call, nothing
        /// is emitted.
        /// </summary>
        /// <param name="message">message to be prepended to output</param>
        private void Dump(string message = "")
        {
            string newMessage = message + $"Current: {ToString(Current)} UndoStack: {ToString(UndoStack)} RedoStack: {ToString(RedoStack)}\n";
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
                return action.GetType().Name + "(hadEffect=" + action.CurrentProgress() + ")";
            }
        }
    }
}
