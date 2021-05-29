using SEE.Net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// Thrown in case an Undo is called although there is no action
    /// that could be undone or if there are conflicting actions
    /// to be undone.
    /// </summary>
    public class UndoImpossible : Exception
    {
        /// <summary>
        /// Constructor providing additional information about the reason
        /// for the exception.
        /// </summary>
        /// <param name="message">additional information</param>
        public UndoImpossible(string message) : base(message)
        { }
    }

    /// <summary>
    /// Thrown in case a Redo is called although there is no action
    /// that could be re-done or if there are conflicting actions
    /// to be re-done.
    /// </summary>
    public class RedoImpossible : Exception
    {
        /// <summary>
        /// Constructor providing additional information about the reason
        /// for the exception.
        /// </summary>
        /// <param name="message">additional information</param>
        public RedoImpossible(string message) : base(message)
        { }
    }

    /// <summary>
    /// Maintains a history of executed reversible actions that can be undone and redone.
    /// </summary>
    public class ActionHistory
    {
        // If a player executes an action that changes the same GameObject as an action
        // of another player, the player that has done the action first
        // cannot perform an undo or redo on that because of a conflict.
        // If the second player undoes the newer change of the object, 
        // the undo of the older change is still not possible because
        // the change is still on the redoHistory of the other player.
        // If the player who executed the newer action undoes the action 
        // and clears his redoHistory by performing another action,
        // the other player can undo his action.

        // Implementation note: This ActionHistory is a bit more complicated than
        // action histories in other contexts because we do not have atomic actions
        // that are either executed or not. Our <see cref="ReversibleAction"/> have
        // a life cycle described by <see cref="ReversibleAction.Progress"/>.
        // They start and receive Update calls until they are completely done.
        // If an action was completely done, the execution continues with a new instance
        // of the same action kind. If an action is undone while in progress state
        // <see cref="ReversibleAction.Progress.InProgress"/>, it has already had
        // some effect that needs to be undone.
        //
        // See also the test cases in <seealso cref="SEETests.TestActionHistory"/> for 
        // additional information.

        public struct GlobalHistoryEntry
        {
            /// <summary>
            /// Represents an entry in the globalHistory.
            /// </summary>
            /// <param name="isOwner">Is the user the owner</param>
            /// <param name="type">The type of the action</param>
            /// <param name="actionID">The ID of the action</param>
            /// <param name="changedObjects">The objects that there changed by this action</param>
            public GlobalHistoryEntry(bool isOwner, HistoryType type, string actionID, List<string> changedObjects)
            {
                IsOwner = isOwner;
                ActionType = type;
                ActionID = actionID;
                ChangedObjects = changedObjects;
            }

            public bool IsOwner { get; }
            public HistoryType ActionType { get; }
            public string ActionID { get; }
            public List<string> ChangedObjects { get; }

        }

        /// <summary>
        /// An enum which sets the type of an action in the history.
        /// </summary>
        public enum HistoryType
        {
            action,
            undoneAction,
        };

        /// <summary>
        /// If a user has done an undo
        /// </summary>
        private bool isRedo = false;

        //NetActionHistory globalActionHistoryNetwork = new NetActionHistory();

        /// <summary>
        /// The actionHistory, which is synchronised through the network on each client.
        /// </summary>
        private List<GlobalHistoryEntry> globalHistory = new List<GlobalHistoryEntry>();

        /// <summary>
        /// Contains all actions executed by the player.
        /// </summary>
        private Stack<ReversibleAction> UndoHistory { get; } = new Stack<ReversibleAction>();

        /// <summary>
        /// Contains all undone actions executed by the player.
        /// </summary>
        private Stack<ReversibleAction> RedoHistory { get; } = new Stack<ReversibleAction>();

        /// <summary>
        /// Checks the invariant that the <see cref="UndoHistory"/> has at most
        /// one action with progress state <see cref="ReversibleAction.Progress.NoEffect"/>
        /// and this action is at the top of <see cref="UndoHistory"/>.
        /// </summary>
        private void AssertAtMostOneActionWithNoEffect()
        {
            UnityEngine.Assertions.Assert.IsTrue
               (UndoHistory.Skip(1).All(action => action.CurrentProgress() != ReversibleAction.Progress.NoEffect));
        }

        /// <summary>
        /// The currently executing action in the undo history.
        /// </summary>
        public ReversibleAction CurrentAction => UndoHistory.Count > 0 ? UndoHistory.Peek() : null;

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
            AssertAtMostOneActionWithNoEffect();
            CurrentAction?.Stop();
            LastActionWithEffect();
            UndoHistory.Push(action);
            action.Awake();
            action.Start();
        }

        /// Calls <see cref="ReversibleAction.Update"/> for the currently executed action of this
        /// action history if there is any. If that action signals that it is complete (via
        /// <see cref="ReversibleAction.Update"/>), a new instance of the same kind as this
        /// action will be created, added to the action history and become the new currently
        /// executed action. The Update is propagated to all clients in the network.
        /// 
        /// If there is no currently executed action, nothing happens.
        /// </summary>
        public void Update()
        {
            ReversibleAction lastAction = CurrentAction;
            if (lastAction != null && lastAction.Update())
            {
                string actionID = lastAction.GetId();
                List<string> changedObjects = lastAction.GetChangedObjects();
                Push(new GlobalHistoryEntry(true, HistoryType.action, actionID, changedObjects));
                new NetActionHistory().Push(HistoryType.action, actionID, changedObjects);
                Execute(lastAction.NewInstance());
            }
        }

        /// <summary>
        /// Pushes new actions to the <see cref="globalHistory"/>.
        /// </summary>
        /// <param name="entry">The action and all of its specific values which are needed for the history</param>
        public void Push(GlobalHistoryEntry entry)
        {
            globalHistory.Add(entry);
        }

        /// <summary>
        /// Replaces the unfinished action in the  <see cref="globalHistory"/> with the finished action.
        /// It is important, because the gameObjects, which are manipulated by the action have to be listed just the same
        /// as the values for the mementos.
        /// </summary>
        /// <param name="oldItem">the old item in the <see cref="globalHistory"/>.</param>
        /// <param name="newItem">the new item in the <see cref="globalHistory"/>.</param>
        /// <param name="isNetwork">whether the function call came from the network</param>
        public void Replace(GlobalHistoryEntry oldItem, GlobalHistoryEntry newItem, bool isNetwork)
        {
            globalHistory[GetIndexOfAction(oldItem.ActionID)] = newItem;
            if (!isNetwork)
            {
                new NetActionHistory().Replace(oldItem.ActionID, oldItem.ActionType, oldItem.ChangedObjects,
                                               newItem.ActionType, newItem.ChangedObjects);
            }
        }

        /// <summary>
        /// Finds the last executed action of a specific player in the <see cref="globalHistory"/>.
        /// </summary>
        /// <param name="type">the type of action the user wants to perform</param>
        /// 
        /// <returns>A GlobalHistoryEntry. If the player has no last action of the given type left, the entry will be empty </returns>
        private GlobalHistoryEntry FindLastActionOfPlayer(HistoryType type)
        {
            for (int i = globalHistory.Count - 1; i >= 0; i--)
            {
                if (type == HistoryType.undoneAction && globalHistory[i].ActionType == HistoryType.undoneAction
                    || type == HistoryType.action && globalHistory[i].ActionType == HistoryType.action
                    && globalHistory[i].IsOwner)
                {
                    return globalHistory[i];
                }
            }
            return new GlobalHistoryEntry();
        }

        /// <summary>
        /// Checks whether the action to be executed has conflicts to another action.
        /// </summary>
        /// <param name="affectedGameObjects">the gameObjects affected by the action to be undone/redone.</param>
        /// <param name="actionId">the ID of the action which possibly has conflicts.</param>
        /// <returns>true, if there are conflicts, else false.</returns>
        private bool ActionHasConflicts(IList<string> affectedGameObjects, string actionId)
        {
            int index = GetIndexOfAction(actionId);
            if (index == -1)
            {
                return false;
            }
            ++index;
            for (int i = index; i < globalHistory.Count; i++)
            {
                foreach (string s in affectedGameObjects)
                {
                    if (globalHistory[i].ChangedObjects != null)
                    {
                        if (globalHistory[i].ChangedObjects.Contains(s) && !globalHistory[i].IsOwner)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes an item from the <see cref="globalHistory"/> depending on its ID.
        /// </summary>
        /// <param name="id">the ID of the action which should be deleted.</param>
        public void DeleteItem(string id)
        {
            globalHistory.Remove(globalHistory.FirstOrDefault(x => x.ActionID.Equals(id)));
        }

        /// <summary>
        /// Undoes the last action with an effect of a specific player.
        /// If a undo isn't possible or no one is remaining the user gets a notification.
        /// </summary>
        public void Undo()
        {
            if (UndoHistory.Count == 0)
            {
                throw new UndoImpossible("Undo not possible, no changes left to undo!");
            }
            CurrentAction.Stop();
            ReversibleAction current = LastActionWithEffect();

            if (current == null)
            {
                return;
            }

            GlobalHistoryEntry lastAction = globalHistory[GetIndexOfAction(current.GetId())];

            if (ActionHasConflicts(current.GetChangedObjects(), current.GetId()))
            {                
                Replace(lastAction, new GlobalHistoryEntry(false, HistoryType.undoneAction, lastAction.ActionID, lastAction.ChangedObjects), false);
                UndoHistory.Pop();
                current.Start();
                throw new UndoImpossible("Undo not possible. Someone else has made a change on the same object.");
            }
            else
            {
                current.Undo();
                RedoHistory.Push(current);
                UndoHistory.Pop();
                DeleteItem(lastAction.ActionID);
                new NetActionHistory().Delete(lastAction.ActionID);

                GlobalHistoryEntry undoneAction = new GlobalHistoryEntry(true, HistoryType.undoneAction, lastAction.ActionID, lastAction.ChangedObjects);
                Push(undoneAction);
                new NetActionHistory().Push(undoneAction.ActionType, undoneAction.ActionID, undoneAction.ChangedObjects);
                isRedo = true;
                Resume(current);
            }
        }

        /// <summary>
        /// Redoes the last undone action of a specific player.
        /// </summary>
        /// <exception cref="RedoImpossible">thrown in there is no action that could be re-done</exception>
        public void Redo()
        {
            GlobalHistoryEntry lastUndoneAction = FindLastActionOfPlayer(HistoryType.undoneAction);
            if (RedoHistory.Count == 0 || lastUndoneAction.ActionID == null)
            {
                throw new RedoImpossible("Redo not possible, no action left to be redone!");
            }
            else
            {
                CurrentAction?.Stop();
                if (ActionHasConflicts(lastUndoneAction.ChangedObjects, lastUndoneAction.ActionID))
                {
                    RedoHistory.Pop();
                    Replace(lastUndoneAction, new GlobalHistoryEntry(false, HistoryType.undoneAction, lastUndoneAction.ActionID, lastUndoneAction.ChangedObjects), false);
                    CurrentAction.Start();
                    throw new RedoImpossible("Redo not possible. Someone else has made a change on the same object.");
                }

                ReversibleAction redoAction = RedoHistory.Pop();
                LastActionWithEffect();
                UndoHistory.Push(redoAction);
                redoAction.Redo();
                Resume(redoAction);

                UnityEngine.Assertions.Assert.IsTrue(RedoHistory.Count == 0
                                 || RedoHistory.Peek().CurrentProgress() != ReversibleAction.Progress.NoEffect);

                GlobalHistoryEntry redoneAction = new GlobalHistoryEntry(true, HistoryType.action, lastUndoneAction.ActionID, lastUndoneAction.ChangedObjects);
                DeleteItem(lastUndoneAction.ActionID);
                new NetActionHistory().Delete(lastUndoneAction.ActionID);
                Push(redoneAction);
                new NetActionHistory().Push(redoneAction.ActionType, redoneAction.ActionID, redoneAction.ChangedObjects);
            }
        }

        /// <summary>
        /// Resumes the execution with a fresh instance of the given <paramref name="action"/> 
        /// if its current progress is <see cref="ReversibleAction.Progress.Completed"/>
        /// or otherwise with <paramref name="action"/> if the current progress
        /// is <see cref="ReversibleAction.Progress.InProgress"/>.
        /// 
        /// Precondition: the current progress of <paramref name="action"/>
        /// is different from <see cref="ReversibleAction.Progress.NoEffect"/>.
        /// </summary>
        /// <param name="action">action to be resumed</param>
        private void Resume(ReversibleAction action)
        {
            UnityEngine.Assertions.Assert.IsTrue(action.CurrentProgress() != ReversibleAction.Progress.NoEffect);
            if (action.CurrentProgress() == ReversibleAction.Progress.Completed)
            {
                // We will resume with a fresh instance of the current action as
                // the (now) current has already been completed.
                action = action.NewInstance();
                UndoHistory.Push(action);
                action.Awake();
            }
            action.Start();
        }

        /// <summary>
        /// Returns the last action on the <see cref="UndoHistory"/> that
        /// has had any effect (preliminary or complete) or null if there is
        /// no such action. 
        /// 
        /// Side effect: All actions at the top of the <see cref="UndoHistory"/> 
        /// are popped off.
        /// </summary>
        /// <returns>the last action on the <see cref="UndoHistory"/> that
        /// has had any effect (preliminary or complete) or null<</returns>
        private ReversibleAction LastActionWithEffect()
        {
            while (UndoHistory.Count > 0)
            {
                ReversibleAction action = UndoHistory.Peek();
                if (action.CurrentProgress() != ReversibleAction.Progress.NoEffect)
                {
                    return action;
                }
                else
                {
                    UndoHistory.Pop();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the index of the last action having the given <paramref name="idOfAction"/>
        /// within <see cref="globalHistory"/> or -1 if there is none.
        /// <param name="idOfAction">the unique ID of the action whose index has to be found</param>
        /// </summary>
        /// <returns>index of the last action having the given <paramref name="idOfAction"/>
        /// within <see cref="globalHistory"/> or -1</returns>
        private int GetIndexOfAction(string idOfAction)
        {
            // Traversing globalHistory backward.
            for (int i = globalHistory.Count - 1; i >= 0; i--)
            {
                if (globalHistory[i].ActionID == idOfAction)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns whether a player has no actions left to be undone
        /// </summary>
        /// <returns>true if no action left, else false.</returns>
        public bool NoActionsLeft()
        {
            return FindLastActionOfPlayer(HistoryType.action).ActionID == null;
        }

        /// <summary>
        /// Returns the number of action that can be undone.
        /// </summary>
        /// <returns>number of un-doable actions</returns>
        public int UndoCount()
        {
            return UndoHistory.Count;
        }

        /// <summary>
        /// Returns the number of action that can be redone.
        /// </summary>
        /// <returns>number of re-doable actions</returns>
        public int RedoCount()
        {
            return RedoHistory.Count;
        }
    }
}