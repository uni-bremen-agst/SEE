using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// The actionList it has an Tupel of a bool Isowner, The type of the Action (Undo Redo Action), the id of the ReversibleAction, the list with the ids of the manipulated GameObjects.
    /// A ringbuffer
    /// </summary>
    private List<Tuple<bool, HistoryType, string, List<string>>> allActionsList = new List<Tuple<bool, HistoryType, string, List<string>>>();

    private List<ReversibleAction> OwnActions = new List<ReversibleAction>();
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
        Push(new Tuple<bool, HistoryType, string, List<string>>(true, HistoryType.action, action.GetId(), null));
        OwnActions.Add(action);
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

        if(activeAction.Update() && activeAction.HadEffect())
        {
            Tuple<bool, HistoryType, string, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
            if (lastAction == null) return;
            DeleteItem(activeAction.GetId(), true);
            lastAction = new Tuple<bool, HistoryType, string, List<string>>(lastAction.Item1, lastAction.Item2, activeAction.GetId(), activeAction.GetChangedObjects());
            Push(lastAction);
            OwnActions.Add(activeAction);
            Execute(activeAction.NewInstance());
        }
    }

    /// <summary>
    /// Pushes new actions to the <see cref="allActionsList"/>
    /// </summary>
    /// <param name="action">The action and all of its specific values which are needed for the history</param>
    private void Push(Tuple<bool, HistoryType, string, List<string>> action)
    {
        allActionsList.Add(action);
    }

    /// <summary>
    /// Finds a specific action by here id from the OwnActions
    /// </summary>
    /// <param name="id">thge id of the action</param>
    /// <returns>the action</returns>
    private ReversibleAction FindById(string id)
    {
        foreach(ReversibleAction it in OwnActions) if(it.GetId().Equals(id)) return it;
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
            if ((type == HistoryType.undoneAction && allActionsList[i].Item2 == HistoryType.undoneAction)
                || (type == HistoryType.action && allActionsList[i].Item2 == HistoryType.action)
                && allActionsList[i].Item1 == true)
            {
                result = allActionsList[i];
                break;
            }
        }
        // Fixme: Outsourcing to another function
        //Find all newer changes that could be a problem to the undo
        //if (count > 1 && result != null) //IF Result == NULL no undo could be performed
        //{
        //    while (true)
        //    {
        //        //Checks if any item from list 1 is in list 2
        //        if (result.Item5?.Where(it => actionList[count].Item5.Contains(it)) != null) //FIXME: Could make some trouble, not sure if it works
        //        {
        //            //results.Add(actionList[i].Item4);
        //            return new Tuple<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>>, bool>(result, true); //Delete this return if you want to give all actions to the caller 
        //        }
        //        if (i == count) break;
        //        if (i < count) i++;
        //    }
        //}
        return result;
    }

    /// <summary>
    /// Deletes all redos of a user
    /// </summary>
    private void DeleteAllRedos() //FIXME Muss auch die andere liste pflegen
    {
        for (int i = 0; i < allActionsList.Count; i++)
        {
            if (allActionsList[i].Item1.Equals(true) && allActionsList[i].Item2.Equals(HistoryType.undoneAction))
            {
                OwnActions.Remove(FindById(allActionsList[i].Item3)); //FIXME: is that uniqe and works?
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
    private void DeleteItem(string id, bool isOwner)
    {
        for (int i = 0; i < allActionsList.Count; i++)
        {
            if (allActionsList[i].Item3.Equals(id))
            {
                allActionsList.RemoveAt(i);
                if(isOwner) OwnActions.Remove(FindById(id)); //FIXME: is that unique and works?
                return;
            }
        }
    }

    /// <summary>
    /// Undoes the last action with an effect of a specific player.
    /// </summary>
    public void Undo()
    {
        Tuple<bool, HistoryType, string, List<string>> lastAction = FindLastActionOfPlayer(true, HistoryType.action);
        if(lastAction == null) return;
        while (!activeAction.HadEffect())
        {
            activeAction.Stop();
            if (allActionsList.Count > 1) //FIXME: Maybe obsolet becaus not multiplayer compatible, should be replaced by lastaction == null -> return
            {
                DeleteItem(lastAction.Item3, lastAction.Item1);
                lastAction = FindLastActionOfPlayer(true, HistoryType.action);
                if (lastAction == null) return;
                activeAction = FindById(lastAction.Item3);
            }
            else
            {
                // Fixme: What should we do, if undo is not possible?
                return;
            }
        }
        activeAction?.Stop();
        activeAction?.Undo();
        DeleteItem(lastAction.Item3, lastAction.Item1);
        Tuple<bool, HistoryType, string, List<string>> undoneAction = new Tuple<bool, HistoryType, string, List<string>>
            (true, HistoryType.undoneAction, lastAction.Item3, lastAction.Item4);
        
        Push(undoneAction);
        OwnActions.Add(activeAction);
        lastAction = FindLastActionOfPlayer(true, HistoryType.action);
        if (lastAction == null) return;
        activeAction = FindById(lastAction.Item3);
        activeAction?.Start();
        isRedo = true;
    }

    /// <summary>
    /// Redoes the last undone action of a specific player.
    /// </summary>
    public void Redo()
    {
        activeAction?.Stop();

        Tuple<bool, HistoryType, string, List<string>> lastUndoneAction = FindLastActionOfPlayer(true, HistoryType.undoneAction);
        if (lastUndoneAction == null) return;
        ReversibleAction temp = FindById(lastUndoneAction.Item3);
        temp.Redo();
        temp.Start();
        Tuple<bool, HistoryType, string, List<string>> redoneAction = new Tuple<bool, HistoryType, string, List<string>>(true, HistoryType.action, lastUndoneAction.Item3, lastUndoneAction.Item4);
        Push(redoneAction);
        activeAction = temp;
        DeleteItem(lastUndoneAction.Item3, lastUndoneAction.Item1);
        OwnActions.Add(temp);
    }

    /// <summary>
    /// Returns the active action of a player
    /// </summary>
    /// <returns>The active action of a player</returns>
    public ReversibleAction getActiveAction()
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
