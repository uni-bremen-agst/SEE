using SEE.Net.Actions;
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

        /// <summary>
        /// An entry describing an executed action in the global action history.
        /// </summary>
        public struct GlobalHistoryEntry
        {
            /// <summary>
            /// Represents an entry in the globalHistory.
            /// </summary>
            /// <param name="isOwner">Is the user the owner</param>
            /// <param name="type">The type of the action</param>
            /// <param name="actionID">The ID of the action</param>
            /// <param name="changedObjects">The objects that there changed by this action</param>
            public GlobalHistoryEntry(bool isOwner, HistoryType type, string actionID, HashSet<string> changedObjects)
            {
                IsOwner = isOwner;
                ActionType = type;
                ActionID = actionID;
                ChangedObjects = changedObjects;
            }

            /// <summary>
            /// This action was triggered by the local player.
            /// </summary>
            public bool IsOwner { get; }
            /// <summary>
            /// Whether the action was executed or undone.
            /// </summary>
            public HistoryType ActionType { get; }
            /// <summary>
            /// The unique ID of the action.
            /// </summary>
            public string ActionID { get; }
            /// <summary>
            /// The unique identifiers of all game objects changed by this action.
            /// This information is the basis to detect conflicting changes.
            /// </summary>
            public HashSet<string> ChangedObjects { get; }
        }

        /// <summary>
        /// An enum which sets the type of an action in the history.
        /// </summary>
        public enum HistoryType
        {
            /// <summary>
            /// Marker for an action that was executed and not undone (it
            /// may have been undone, but then it was re-done again).
            /// </summary>
            Action,
            /// <summary>
            /// Marker for an action that was executed but then undone (and
            /// since then not again re-done).
            /// </summary>
            UndoneAction,
        };

        /// <summary>
        /// The actionHistory, which is synchronised through the network on each client.
        ///
        /// FIXME: This list should be forgetting, that is, it should not grow
        /// without limit. Otherwise we will run into performance problems if
        /// the history is large and the actions have changed many objects.
        /// See the details in <see cref="ActionHasConflicts"/>.
        /// Quite likely a dictionary indexed by the action ids would be a
        /// better data structure, because of the faster look up.
        /// </summary>
        private readonly List<GlobalHistoryEntry> globalHistory = new List<GlobalHistoryEntry>();

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
            // Whenever a new action is excuted, we consider the redo history lost.
            DeleteAllRedos();
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
                // Note: An action is put to the global history only when
                // its execution is completed, that is, if its Update
                // yields true. Here is the place, where this is the fact.
                AddToGlobalHistory(lastAction);
                Execute(lastAction.NewInstance());
            }
        }

        /// <summary>
        /// Adds <paramref name="action"/> to the global history, that is,
        /// to <see cref="globalHistory"/> and also via the network on all clients.
        ///
        /// This is the counterpart of <see cref="RemoveFromGlobalHistory(GlobalHistoryEntry)"/>.
        /// </summary>
        /// <param name="action">action to be added</param>
        private void AddToGlobalHistory(ReversibleAction action)
        {
            string actionID = action.GetId();
            HashSet<string> changedObjects = action.GetChangedObjects();
            Push(new GlobalHistoryEntry(true, HistoryType.Action, actionID, changedObjects));
            new NetActionHistory().Push(HistoryType.Action, actionID, changedObjects);
        }

        /// <summary>
        /// Removes <paramref name="action"/> from the global history, that is,
        /// from <see cref="globalHistory"/> and also via the network on all clients.
        ///
        /// This is the counterpart of <see cref="AddToGlobalHistory(ReversibleAction)"/>.
        /// </summary>
        /// <param name="action">action to be added</param>
        private void RemoveFromGlobalHistory(GlobalHistoryEntry action)
        {
            RemoveAction(action.ActionID);
            new NetActionHistory().Delete(action.ActionID);
        }

        /// <summary>
        /// Clears <see cref="RedoHistory"/> and removes all the local player's
        /// undone actions in <see cref="globalHistory"/> both locally and for
        /// all clients in the network.
        /// </summary>
        private void DeleteAllRedos()
        {
            for (int i = 0; i < globalHistory.Count; i++)
            {
                if (globalHistory[i].IsOwner && globalHistory[i].ActionType == HistoryType.UndoneAction)
                {
                    new NetActionHistory().Delete(globalHistory[i].ActionID);
                    globalHistory.RemoveAt(i);
                    i--;
                }
            }
            RedoHistory.Clear();
        }

        /// <summary>
        /// Pushes <paramref name="entry"/> to the <see cref="globalHistory"/>.
        /// The addition remains local, that is, is not propagated to all clients.
        /// </summary>
        /// <param name="entry">The action and all of its specific values needed for the history</param>
        public void Push(GlobalHistoryEntry entry)
        {
            globalHistory.Add(entry);
        }

        /// <summary>
        /// Removes the action with given <paramref name="id"/> from the <see cref="globalHistory"/>.
        /// </summary>
        /// <param name="id">the ID of the action that should be removed</param>
        public void RemoveAction(string id)
        {
            globalHistory.Remove(globalHistory.FirstOrDefault(x => x.ActionID.Equals(id)));
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
        /// <returns>A GlobalHistoryEntry. If the player has no last action of the given type left, the entry will be empty</returns>
        private GlobalHistoryEntry FindLastActionOfPlayer(HistoryType type)
        {
            for (int i = globalHistory.Count - 1; i >= 0; i--)
            {
                if (type == HistoryType.UndoneAction && globalHistory[i].ActionType == HistoryType.UndoneAction
                    || type == HistoryType.Action && globalHistory[i].ActionType == HistoryType.Action
                    && globalHistory[i].IsOwner)
                {
                    return globalHistory[i];
                }
            }
            return new GlobalHistoryEntry();
        }

        /// <summary>
        /// Checks whether the action with the given <paramref name="actionId"/> present
        /// in <see cref="globalHistory"/> has a conflict to another action in the
        /// <see cref="globalHistory"/>. Two actions have a conflict if their two
        /// sets of modified game objects overlap.
        /// If the action is not contained in <see cref="globalHistory"/>, it
        /// cannot have a conflict.
        /// </summary>
        /// <param name="affectedGameObjects">the gameObjects modified by the action</param>
        /// <param name="actionId">the ID of the action</param>
        /// <returns>true if there are conflicts</returns>
        private bool ActionHasConflicts(HashSet<string> affectedGameObjects, string actionId)
        {
            if (affectedGameObjects.Count == 0)
            {
                return false;
            }
            int index = GetIndexOfAction(actionId);
            if (index == -1)
            {
                return false;
            }
            ++index;
            for (int i = index; i < globalHistory.Count; i++)
            {
                if (!globalHistory[i].IsOwner && affectedGameObjects.Overlaps(globalHistory[i].ChangedObjects))
                {
                    return true;
                }
            }
            return false;
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
            CurrentAction.Stop();
            ReversibleAction current = LastActionWithEffect();

            if (current == null)
            {
                return;
            }

            if (ActionHasConflicts(current.GetChangedObjects(), current.GetId()))
            {
                GlobalHistoryEntry lastAction = globalHistory[GetIndexOfAction(current.GetId())];
                Replace(lastAction, new GlobalHistoryEntry(false, HistoryType.UndoneAction, lastAction.ActionID, lastAction.ChangedObjects), false);
                UndoHistory.Pop();
                current = LastActionWithEffect();
                if (current != null)
                {
                    Resume(current);
                }
                throw new UndoImpossible("Undo not possible. Someone else has made a change on the same object.");
            }
            else
            {
                current.Undo();
                RedoHistory.Push(current);
                UndoHistory.Pop();

                /// The global action history contains only those actions whose
                /// execution is completed. The current action could still
                /// be running (we know that it has already had an effect,
                /// that is, its current progress cannot be <see cref="ReversibleAction.Progress.NoEffect"/>,
                /// otherwise we would not have arrived here). If it is still
                /// running, it cannot be found in the global history.
                if (current.CurrentProgress() == ReversibleAction.Progress.Completed)
                {
                    /// current is a fully executed action contained in <see cref="globalHistory"/>
                    GlobalHistoryEntry lastAction = globalHistory[GetIndexOfAction(current.GetId())];
                    RemoveFromGlobalHistory(lastAction);

                    GlobalHistoryEntry undoneAction = new GlobalHistoryEntry(true, HistoryType.UndoneAction, lastAction.ActionID, lastAction.ChangedObjects);
                    Push(undoneAction);
                    new NetActionHistory().Push(undoneAction.ActionType, undoneAction.ActionID, undoneAction.ChangedObjects);
                }
                // current has been undone and moved on the RedoStack.
                // We will resume with the next top-element of the stack that
                // has had an effect if there is any.
                current = LastActionWithEffect();
                if (current != null)
                {
                    Resume(current);
                }
            }
        }

        /// <summary>
        /// Redoes the last undone action of a specific player.
        /// </summary>
        /// <exception cref="RedoImpossible">thrown in there is no action that could be re-done</exception>
        public void Redo()
        {
            if (RedoHistory.Count == 0)
            {
                throw new RedoImpossible("Redo not possible, no action left to be redone!");
            }
            else
            {
                CurrentAction?.Stop();
                // If the action to be redone has not been executed completely,
                // it will not be contained in the global history, in which case
                // lastUndoneAction.ActionID will be null.
                GlobalHistoryEntry lastUndoneAction = FindLastActionOfPlayer(HistoryType.UndoneAction);
                if (lastUndoneAction.ActionID != null && ActionHasConflicts(lastUndoneAction.ChangedObjects, lastUndoneAction.ActionID))
                {
                    RedoHistory.Pop();
                    Replace(lastUndoneAction, new GlobalHistoryEntry(false, HistoryType.UndoneAction, lastUndoneAction.ActionID, lastUndoneAction.ChangedObjects), false);
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

                // Only if the action to be redone has been executed completely
                // and is, thus, contained in the global history, we need to update
                // the global history.
                if (lastUndoneAction.ActionID != null)
                {
                    GlobalHistoryEntry redoneAction = new GlobalHistoryEntry(true, HistoryType.Action, lastUndoneAction.ActionID, lastUndoneAction.ChangedObjects);
                    RemoveAction(lastUndoneAction.ActionID);
                    new NetActionHistory().Delete(lastUndoneAction.ActionID);
                    Push(redoneAction);
                    new NetActionHistory().Push(redoneAction.ActionType, redoneAction.ActionID, redoneAction.ChangedObjects);
                }
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