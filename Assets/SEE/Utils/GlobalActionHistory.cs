using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GlobalActionHistory
{
    /// <summary>
    /// An enum which sets the type of an action in the history.
    /// </summary>
    private enum HistoryType
    {
        action,
        undoneAction,
    };

    /// <summary>
    /// If a user has done a undo
    /// </summary>
    private bool isRedo = false;

    /// <summary>
    /// The actionList it has an Tupel of a bool Isowner, The type of the Action (Undo Redo Action), the ReversibleAction, the list with the ids of the manipulated GameObjects.
    /// A ringbuffer
    /// </summary>
    private List<Tuple<bool, HistoryType, ReversibleAction, List<string>>> allActionsList = new List<Tuple<bool, HistoryType, ReversibleAction, List<string>>>();

    /// <summary>
    /// Contains the Active Action from each Player needs to be updated with each undo/redo/action
    /// </summary>
    //private Dictionary<string, ReversibleAction> allActiveActions = new Dictionary<string, ReversibleAction>();
    private ReversibleAction activeAction = null;
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
        //GetActiveAction(key)?.Stop();
        activeAction?.Stop();
        Push(new Tuple<bool, HistoryType, ReversibleAction, List<string>>(true, HistoryType.action, action, null));
        //SetActiveAction(key, action);
        activeAction = action;
        action.Awake();
        action.Start();

        // Whenever a new action is excuted, we consider the redo stack lost.
        if (isRedo)
        {
            DeleteAllRedos();
        }
    }

    /// <summary>
    /// Calls the update method of each active action.
    /// </summary>
    public void Update()
    {
        /*for (int i = 0; i < allActiveActions.Count; i++)
        {
            if (allActiveActions.ElementAt(i).Value.Update() && allActiveActions.ElementAt(i).Value.HadEffect())
            {
                // Overwrites the running action with finished reversibleAction - necessary for listing all manipulated gameObjects. 
                Tuple<string, HistoryType, ReversibleAction, List<string>> lastAction = FindLastActionOfPlayer(allActiveActions.ElementAt(i).Key, HistoryType.action).Item1;
                if (lastAction == null) return;
                DeleteItem(allActiveActions.ElementAt(i).Value.GetId());
                lastAction = new Tuple<string, HistoryType, ReversibleAction, List<string>>(lastAction.Item1, lastAction.Item2, allActiveActions.ElementAt(i).Value, allActiveActions.ElementAt(i).Value.GetChangedObjects());
                Push(lastAction);
                Execute(allActiveActions.ElementAt(i).Value.NewInstance(), allActiveActions.ElementAt(i).Key);
            }
        }*/

        if (activeAction.Update() && activeAction.HadEffect())
        {
            Tuple<bool, HistoryType, ReversibleAction, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
            if (lastAction == null) return;
            DeleteItem(activeAction.GetId());
            lastAction = new Tuple<bool, HistoryType, ReversibleAction, List<string>>(lastAction.Item1, lastAction.Item2, activeAction, activeAction.GetChangedObjects());
            Push(lastAction);
            Execute(activeAction.NewInstance());
        }
    }

    /// <summary>
    /// Pushes new actions to the <see cref="allActionsList"/>
    /// </summary>
    /// <param name="action">The action and all of its specific values which are needed for the history</param>
    private void Push(Tuple<bool, HistoryType, ReversibleAction, List<string>> action)
    {
        allActionsList.Add(action);
    }

    /// <summary>
    /// Finds the last executed action of a specific player.
    /// </summary>
    /// <param name="playerID">The player that wants to perform an undo/redo</param>
    /// <param name="type">the type of action he wants to perform</param>
    /// <returns>A tuple of the latest users action and if any later done action blocks the undo (True if some action is blocking || false if not)</returns>  
    /// Returns as second in the tuple that so each action could check it on its own >> List<ReversibleAction>> Returns Null if no action was found
    private Tuple<bool, HistoryType, ReversibleAction, List<string>> FindLastActionOfPlayer(bool isOwner, HistoryType type)
    {
        Tuple<bool, HistoryType, ReversibleAction, List<string>> result = null;

        for (int i = allActionsList.Count - 1; i >= 0; i--)
        {
            if ((type == HistoryType.undoneAction && allActionsList[i].Item2 == HistoryType.undoneAction)
                || (type == HistoryType.action && allActionsList[i].Item2 == HistoryType.action)
                && allActionsList[i].Item1 == true)
            {
                result = allActionsList[i];
                break;
            }
        }
        return result;
    }

    private bool ActionHasConflicts(List<string> affectedGameObjects)
    {
        //Find all newer changes that could be a problem to the undo
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
    private void DeleteItem(string id)
    {
        for (int i = 0; i < allActionsList.Count; i++)
        {
            if (allActionsList[i].Item3.GetId().Equals(id))
            {
                allActionsList.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Undoes the last action with an effect of a specific player.
    /// </summary>
    public void Undo()
    {
        Tuple<bool, HistoryType, ReversibleAction, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
        if (lastAction == null) return;
        while (!activeAction.HadEffect())
        {
            activeAction.Stop();
            if (allActionsList.Count > 1) //FIXME: Maybe obsolet becaus not multiplayer compatible, should be replaced by lastaction == null -> return
            {
                DeleteItem(lastAction.Item3.GetId());
                lastAction = FindLastActionOfPlayer(true, HistoryType.action);
                if (lastAction == null) return;
                activeAction = lastAction.Item3;
            }
            else
            {
                // Fixme: What should we do, if undo is not possible?
                return;
            }
        }

        // Fixme: Right place ?
        if (ActionHasConflicts(activeAction.GetChangedObjects()))
        {
            // Fixme: Error
        }
        else
        {
            activeAction?.Stop();
            activeAction?.Undo();
            DeleteItem(lastAction.Item3.GetId());
            Tuple<bool, HistoryType, ReversibleAction, List<string>> undoneAction = new Tuple<bool, HistoryType, ReversibleAction, List<string>>
                (true, HistoryType.undoneAction, lastAction.Item3, lastAction.Item4);

            Push(undoneAction);
            lastAction = FindLastActionOfPlayer(true, HistoryType.action);
            if (lastAction == null) return;
            activeAction = lastAction.Item3;
            activeAction?.Start();
            isRedo = true;
        }
    }

    /// <summary>
    /// Redoes the last undone action of a specific player.
    /// </summary>
    public void Redo()
    {
        activeAction?.Stop();
        Tuple<bool, HistoryType, ReversibleAction, List<string>> lastUndoneAction = FindLastActionOfPlayer(true, HistoryType.undoneAction);
        if (lastUndoneAction == null) return;

        // Fixme: Right place ?
        if (ActionHasConflicts(lastUndoneAction.Item4))
        {
            // Fixme: Error
        }

        lastUndoneAction.Item3.Redo();
        Tuple<bool, HistoryType, ReversibleAction, List<string>> redoneAction = new Tuple<bool, HistoryType, ReversibleAction, List<string>>(true, HistoryType.action, lastUndoneAction.Item3, lastUndoneAction.Item4);
        Push(redoneAction);
        lastUndoneAction.Item3.Start();

        activeAction = lastUndoneAction.Item3;
        DeleteItem(lastUndoneAction.Item3.GetId());
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
    /// Returns wether a player has no Actions left to be undone
    /// </summary>
    /// <returns>True if no action left</returns>
    public bool NoActionsLeft()
    {
        return FindLastActionOfPlayer(true, HistoryType.action) == null;
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
    /// Returns the Active Action for the Player
    /// </summary>
    /// <param name="player">The Player that performs an Action</param>
    /// <returns>The active action || null if key not in dictionary</returns>
    /*public ReversibleAction GetActiveAction(string player)
    {
        try
        {
            return allActiveActions[player];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    } 

    /// <summary>
    /// Sets the active <paramref name="action"/> of a specific <paramref name="player"/>. 
    /// </summary>
    /// <param name="player">The player to set the active action</param>
    /// <param name="action">the new active action</param>
    private void SetActiveAction(string player, ReversibleAction action)
    {
        try
        {
            allActiveActions[player] = action;
        }
        catch (KeyNotFoundException)
        {
            allActiveActions.Add(player, action);
        }
    } */

}
