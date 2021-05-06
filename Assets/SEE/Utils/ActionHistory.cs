using SEE.Net;
using SEE.Utils;
using System;
using System.Collections.Generic;
<<<<<<< HEAD
=======
using System.Linq;
using System.Text;
>>>>>>> origin/master
using UnityEngine;

namespace Assets.SEE.Utils
{
    public class ActionHistory
    {
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
<<<<<<< HEAD
        /// An enum which sets the type of an action in the history.
        /// </summary>
        public enum HistoryType
        {
            action,
            undoneAction,
        };

        /// <summary>
        /// If a user has done a undo
        /// </summary>
        private bool isRedo = false;

        /// <summary>
        /// The actionList it has an Tupel of a bool Isowner, The type of the Action (Undo Redo Action), the id of the ReversibleAction, the list with the ids of the manipulated GameObjects.
        /// A ringbuffer
        /// </summary>
        private List<Tuple<bool, HistoryType, string, List<string>>> allActionsList = new List<Tuple<bool, HistoryType, string, List<string>>>();

        /// <summary>
        /// Contains all actions executed by the player.
=======
        /// The history of actions that have been executed (and have not yet been undone). The currently
        /// executed action is the top element of this stack.
        /// Note: At the top of <see cref="UndoStack"/> there could be (at most) one action
        /// with progress state <see cref="ReversibleAction.Progress.NoEffect"/>. Whenever
        /// Update returns true for the currently executed action, that action is considered
        /// complete and the execution resumes with a fresh instance of the same kind
        /// (created via Current.NewInstance()), that is, that fresh instance is added
        /// to the UndoStack with the progress state <see cref="ReversibleAction.Progress.NoEffect"/>.
        /// The action with this progress state will be popped off again whenever a new
        /// action is added via Execute or an undone action on the RedoStack is redone via Redo.
>>>>>>> origin/master
        /// </summary>
        private List<ReversibleAction> ownActions = new List<ReversibleAction>();

        /// <summary>
<<<<<<< HEAD
        /// Contains the Active Action from each Player needs to be updated with each undo/redo/action
=======
        /// The history of actions that have been undone.
        /// 
        /// Invariant: all actions on <see cref="RedoStack"/> will have progress state
        /// <see cref="ReversibleAction.Progress.InProgress"/> or 
        /// <see cref="ReversibleAction.Progress.Completed"/> but never
        /// <see cref="ReversibleAction.Progress.NoEffect"/>.
>>>>>>> origin/master
        /// </summary>
        private ReversibleAction activeAction = null;

        /// <summary>
        /// The maximal size of the action history.
        /// </summary>
        private const int historySize = 100;

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
        /// <param name="ignoreRedoDeletion">Run with out deltetion of redos</param>
        public void Execute(ReversibleAction action, bool ignoreRedoDeletion = false)
        {
<<<<<<< HEAD
            activeAction?.Stop();
            Push(new Tuple<bool, HistoryType, string, List<string>>(true, HistoryType.action, action.GetId(), null));
            new GlobalActionHistoryNetwork().Push(HistoryType.action, action.GetId(), null);
            ownActions.Add(action);
            activeAction = action;
=======
            AssertAtMostOneActionWithNoEffect();
            Current?.Stop();
            // There may be an action that has not had any effect yet.
            // It needs to be popped off the UndoStack.
            LastActionWithEffect();
            UndoStack.Push(action);
>>>>>>> origin/master
            action.Awake();
            action.Start();

            // Whenever a new action is excuted, we consider the redo stack lost.
            if (!ignoreRedoDeletion && isRedo)
            {
                DeleteAllRedos();
            }
        }

        /// <summary>
<<<<<<< HEAD
        /// Calls the update method of each active action.
=======
        /// Checks the invariant that the <see cref="UndoStack"/> has at most
        /// one action with progress state <see cref="ReversibleAction.Progress.NoEffect"/>
        /// and this action is at the top of <see cref="UndoStack"/>.
        /// </summary>
        private void AssertAtMostOneActionWithNoEffect()
        {
            UnityEngine.Assertions.Assert.IsTrue
                (UndoStack.Skip(1).All(action => action.CurrentProgress() != ReversibleAction.Progress.NoEffect));
        }

        /// <summary>
        /// Calls <see cref="ReversibleAction.Update"/> for the currently executed action of this 
        /// action history if there is any. If that action signals that it is complete (via
        /// <see cref="ReversibleAction.Update"/>), a new instance of the same kind as this
        /// action will be created, added to the action history and become the new currently
        /// executed action. If there is no currently executed action, nothing happens.
>>>>>>> origin/master
        /// </summary>
        public void Update()
        {
            if (activeAction.Update() && activeAction.HadEffect())
            {
                Tuple<bool, HistoryType, string, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
                if (lastAction == null) return;
                DeleteItem(activeAction.GetId(), true);
                new GlobalActionHistoryNetwork().Delete(activeAction.GetId());
                lastAction = new Tuple<bool, HistoryType, string, List<string>>(lastAction.Item1, lastAction.Item2, activeAction.GetId(), activeAction.GetChangedObjects());
                Push(lastAction);
                ownActions.Add(activeAction);
                new GlobalActionHistoryNetwork().Push(lastAction.Item2, lastAction.Item3, ListToString(lastAction.Item4));
                Execute(activeAction.NewInstance());
            }
        }

        /// <summary>
<<<<<<< HEAD
        /// Pushes new actions to the <see cref="allActionsList"/>
=======
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
>>>>>>> origin/master
        /// </summary>
        /// <param name="action">The action and all of its specific values which are needed for the history</param>
        public void Push(Tuple<bool, HistoryType, string, List<string>> action)
        {
            if (allActionsList.Count >= historySize)
            {
                allActionsList.RemoveAt(0);
                ownActions.Remove(FindById(allActionsList[0].Item3));
            }
            allActionsList.Add(action);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldItem"></param>
        /// <param name="newItem"></param>
        public void Replace(Tuple<bool, HistoryType, string, List<string>> oldItem, Tuple<bool, HistoryType, string, List<string>> newItem, bool isNetwork)
        {
            int index = GetIndexOfAction(oldItem.Item3);  //FIXME: OwnAction msus evtl auch geloescht werden
            allActionsList[index] = newItem;
            if(!isNetwork) new GlobalActionHistoryNetwork().Replace(oldItem.Item2, oldItem.Item3, ListToString(oldItem.Item4), newItem.Item2, ListToString(newItem.Item4));
        }

        /// <summary>
        /// Finds a specific action by here id from the OwnActions
        /// </summary>
        /// <param name="id">thge id of the action</param>
        /// <returns>the action</returns>
        private ReversibleAction FindById(string id)
        {
            foreach (ReversibleAction it in ownActions) if (it.GetId().Equals(id)) return it;
            return null;
        }

        /// <summary>
        /// Finds the last executed action of a specific player.
        /// </summary>
        /// <param name="playerID">The player that wants to perform an undo/redo</param>
        /// <param name="type">the type of action he wants to perform</param>
        /// <returns>A tuple of the latest users action and if any later done action blocks the undo (True if some action is blocking || false if not)</returns>  
        /// Returns as second in the tuple that so each action could check it on its own >> List<ReversibleAction>> Returns Null if no action was found
        private Tuple<bool, HistoryType, string, List<string>> FindLastActionOfPlayer(bool isOwner, HistoryType type)
        {
            Tuple<bool, HistoryType, string, List<string>> result = null;

            for (int i = allActionsList.Count - 1; i >= 0; i--)
            {
<<<<<<< HEAD
                if (type == HistoryType.undoneAction && allActionsList[i].Item2 == HistoryType.undoneAction
                    || type == HistoryType.action && allActionsList[i].Item2 == HistoryType.action
                    && allActionsList[i].Item1 == true)
                {
                    result = allActionsList[i];
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks whether the action to be executed has conflicts to another action.
        /// </summary>
        /// <param name="affectedGameObjects">the gameObjects affected by the action to be undone/redone.</param>
        /// <returns>true, if there are conflicts, else false</returns>
        private bool ActionHasConflicts(List<string> affectedGameObjects)
        {
            int index = GetIndexOfAction(activeAction.GetId());
            if (index == -1)
            {
                Debug.Log("ERROR IN GETCOUNT");
                return false;
            }
            index++;
            for (int i = index ; i < allActionsList.Count; i++)
            {
                foreach (string s in affectedGameObjects)
                {
                    if (allActionsList[i].Item4 != null)
                    {
                        if (allActionsList[i].Item4.Contains(s) && allActionsList[i].Item1 == false)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes all redos of a user
        /// </summary>
        private void DeleteAllRedos()
        {
            for (int i = 0; i < allActionsList.Count; i++)
            {
                if (allActionsList[i].Item1.Equals(true) && allActionsList[i].Item2.Equals(HistoryType.undoneAction))
                {
                    ownActions.Remove(FindById(allActionsList[i].Item3));
                    new GlobalActionHistoryNetwork().Delete(allActionsList[i].Item3);
                    allActionsList.RemoveAt(i);
                    i--;
                }
                isRedo = false;
            }
        }

        /// <summary>
        /// Deletes an item from the action list depending on its id.
        /// </summary>
        /// <param name="id">the id of the action which should be deleted</param>
        public void DeleteItem(string id, bool isOwner)
        {
            for (int i = 0; i < allActionsList.Count; i++)
            {
                if (allActionsList[i].Item3.Equals(id))
                {
                    allActionsList.RemoveAt(i);
                    if (isOwner) ownActions.Remove(FindById(id));
                    return;
=======
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
                UndoStack.Peek().Stop();
                ReversibleAction current = LastActionWithEffect();
                if (current != null)
                {
                    // assert: current has had an effect
                    current.Undo();
                    RedoStack.Push(current);
                    UndoStack.Pop();
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
                UndoStack.Push(action);
                action.Awake();
            }
            action.Start();
        }

        /// <summary>
        /// Returns the last action on the <see cref="UndoStack"/> that
        /// has had any effect (preliminary or complete) or null if there is
        /// no such action. 
        /// 
        /// Side effect: All actions at the top of the <see cref="UndoStack"/> 
        /// are popped off.
        /// </summary>
        /// <returns>the last action on the <see cref="UndoStack"/> that
        /// has had any effect (preliminary or complete) or null<</returns>
        private ReversibleAction LastActionWithEffect()
        {
            while (UndoStack.Count > 0)
            {
                ReversibleAction action = UndoStack.Peek();
                if (action.CurrentProgress() != ReversibleAction.Progress.NoEffect)
                {
                    return action;
                }
                else
                {
                    UndoStack.Pop();
>>>>>>> origin/master
                }
            }
            return null;
        }

        /// <summary>
<<<<<<< HEAD
        /// Undoes the last action with an effect of a specific player.
=======
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
>>>>>>> origin/master
        /// </summary>
        public void Undo()
        {
            Tuple<bool, HistoryType, string, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
            if (lastAction == null) return;
            while (!activeAction.HadEffect())
            {
<<<<<<< HEAD
                activeAction.Stop();
                DeleteItem(lastAction.Item3, lastAction.Item1);
                new GlobalActionHistoryNetwork().Delete(lastAction.Item3);
                lastAction = FindLastActionOfPlayer(true, HistoryType.action);
                if (lastAction == null) return;
                activeAction = FindById(lastAction.Item3);
            }
            activeAction?.Stop();
            if (ActionHasConflicts(activeAction.GetChangedObjects()))
            {
                Debug.LogWarning("Undo not possible, someone else had made a change on the same object!");
                Replace(lastAction, new Tuple<bool, HistoryType, string, List<string>>(false, HistoryType.undoneAction, lastAction.Item3, lastAction.Item4), false);
                activeAction = FindById(FindLastActionOfPlayer(true,HistoryType.action).Item3);
                activeAction?.Start();
                return;
=======
                Current?.Stop();
                // The last undone action becomes the currently executed action again.
                // This action may have state <see cref="ReversibleAction.Progress.InProgress"/>
                // or <see cref="ReversibleAction.Progress.Completed"/>.
                ReversibleAction redoAction = RedoStack.Pop();
                // Clear the UndoStack from actions without any effect.
                LastActionWithEffect();
                UndoStack.Push(redoAction);
                redoAction.Redo();
                Resume(redoAction);

                UnityEngine.Assertions.Assert.IsTrue(RedoStack.Count == 0
                             || RedoStack.Peek().CurrentProgress() != ReversibleAction.Progress.NoEffect);
>>>>>>> origin/master
            }
            else
            {
                activeAction?.Undo();
                DeleteItem(lastAction.Item3, lastAction.Item1);
                new GlobalActionHistoryNetwork().Delete(lastAction.Item3);
                Tuple<bool, HistoryType, string, List<string>> undoneAction = new Tuple<bool, HistoryType, string, List<string>>
                    (true, HistoryType.undoneAction, lastAction.Item3, lastAction.Item4);

                Push(undoneAction);
                ownActions.Add(activeAction);
                new GlobalActionHistoryNetwork().Push(undoneAction.Item2, undoneAction.Item3, ListToString(undoneAction.Item4));

                lastAction = FindLastActionOfPlayer(true, HistoryType.action);
                if (lastAction == null) return;
                activeAction = FindById(lastAction.Item3);
                
                Execute(activeAction.NewInstance(), true);
                isRedo = true;

            }
        }

        /// <summary>
        /// Redoes the last undone action of a specific player.
        /// </summary>
        public void Redo()
        { 
            Tuple<bool, HistoryType, string, List<string>> lastUndoneAction = FindLastActionOfPlayer(true, HistoryType.undoneAction);
            if (lastUndoneAction == null) return;
            activeAction?.Stop();
            if (ActionHasConflicts(lastUndoneAction.Item4))
            {
                Replace(lastUndoneAction, new Tuple<bool, HistoryType, string, List<string>>(false, HistoryType.undoneAction, lastUndoneAction.Item3, lastUndoneAction.Item4), false);
                Debug.LogWarning("Redo not possible, someone else had made a change on the same object!");
                activeAction = FindById(FindLastActionOfPlayer(true, HistoryType.action).Item3);
                activeAction?.Start();
                return;
            }
            ReversibleAction temp = FindById(lastUndoneAction.Item3);
            temp.Redo();
           
            Tuple<bool, HistoryType, string, List<string>> redoneAction = new Tuple<bool, HistoryType, string, List<string>>(true, HistoryType.action, lastUndoneAction.Item3, lastUndoneAction.Item4);
            DeleteItem(lastUndoneAction.Item3, lastUndoneAction.Item1);
            new GlobalActionHistoryNetwork().Delete(lastUndoneAction.Item3);
            Push(redoneAction);
            new GlobalActionHistoryNetwork().Push(redoneAction.Item2, redoneAction.Item3, ListToString(redoneAction.Item4));
            activeAction = temp;
            ownActions.Add(temp);
            Execute(activeAction.NewInstance(), true);

        }

        /// <summary>
        /// Returns the active action of a player
        /// </summary>
        /// <returns>The active action of a player</returns>
        public ReversibleAction GetActiveAction()
        {
            return activeAction;
        }

        /// <summary>
        /// Gets the number of all actions which are executed by other users after the action with the id <paramref name="idOfAction"/>.
        /// </summary>
        /// <returns>the number of newer actions than that with the id <paramref name="idOfAction"/>, which are not executed by the owner.</returns>
        private int GetIndexOfAction(string idOfAction)
        {
            for(int i = allActionsList.Count-1; i >= 0; i--)
            {
                if (allActionsList[i].Item3.Equals(idOfAction)) return  i;
            }
            return -1;
        }

        /// <summary>
        /// Returns wether a player has no Actions left to be undone
        /// </summary>
<<<<<<< HEAD
        /// <returns>True if no action left</returns>
        public bool NoActionsLeft()
        {
            return FindLastActionOfPlayer(true, HistoryType.action) == null;
=======
        /// <param name="message">message to be prepended to output</param>
#pragma warning disable IDE0051 // Dump is not used
        private void Dump(string message = "")
#pragma warning restore IDE0051 // Dump is not used
        {
            string newMessage = message + $"UndoStack: {ToString(UndoStack)} RedoStack: {ToString(RedoStack)}\n";
            if (previousMessage != newMessage)
            {
                previousMessage = newMessage;
                Debug.Log(previousMessage);
            }
>>>>>>> origin/master
        }

        /// <summary>
        /// Returns wether a player has some undone actions left
        /// </summary>
        /// <returns>True if none undone actions left</returns>
        public bool NoUndoneActionsLeft()
        {
            return FindLastActionOfPlayer(true, HistoryType.undoneAction) == null;
        }

        /// <summary>
        /// Converts a List of gameObjectIds to a single comma seperated string for sending it to other clients.
        /// </summary>
        /// <param name="gameObjectIds">the gameObjectIds</param>
        /// <returns>a single comma seperated string of all gameObjectIds.</returns>
        private string ListToString(List<string> gameObjectIds)
        {
            string result = "";
            if (gameObjectIds == null) return null;
            foreach(string s in gameObjectIds)
            {
                result += s + ",";
            }
            if (result != "" && result != null)
            {
<<<<<<< HEAD
                return result.Substring(0, result.Length - 1);
=======
                return action.GetType().Name + "(hadEffect=" + action.CurrentProgress() + ")";
>>>>>>> origin/master
            }
            else return null;
        }
    }
}