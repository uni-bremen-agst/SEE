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
    /// 
    /// </summary>
    private bool isRedo = false; //FIXME: IF we want to implement this we need an dictionary for each player otherwise it will destroy the funtion in multiplayer 

    /// <summary>
    /// The actionList it has an Tupel of the time as it was performed, Player ID, The type of the Action (Undo Redo Action), the ReversibleAction, and the list with the ids of the manipulated GameObjects
    /// A ringbuffer
    /// </summary>
    private List<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>> allActionsList = new List<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>>();

    /// <summary>
    /// Contains the Active Action from each Player needs to be updated with each undo/redo/action
    /// </summary>
    private Dictionary<string, ReversibleAction> allActiveActions = new Dictionary<string, ReversibleAction>();

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
    public void Execute(ReversibleAction action, string key)
    {
        GetActiveAction(key)?.Stop();
        Push(new Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>(DateTime.Now, key, HistoryType.action, action, null, action.Id()));
        SetActiveAction(key, action);
        action.Awake();
        action.Start();

        // Whenever a new action is excuted, we consider the redo stack lost.
        if (isRedo) DeleteAllRedos(key);
    }

    /// <summary>
    /// Calls the update method of each active action.
    /// </summary>
    public void Update()
    {
        for (int i = 0; i < allActiveActions.Count; i++)
        {
            if (allActiveActions.ElementAt(i).Value.Update() && allActiveActions.ElementAt(i).Value.HadEffect())
            {
                // Overwrites the running action with finished reversibleAction - necessary for listing all manipulated gameObjects. 
                Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> lastAction = FindLastActionOfPlayer(allActiveActions.ElementAt(i).Key, HistoryType.action).Item1;
                DeleteItem(allActiveActions.ElementAt(i).Key, lastAction.Item6);
                lastAction = new Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>(DateTime.Now, lastAction.Item2, lastAction.Item3, allActiveActions.ElementAt(i).Value, allActiveActions.ElementAt(i).Value.GetChangedObjects(), allActiveActions.ElementAt(i).Value.Id());
                Push(lastAction);
                Execute(allActiveActions.ElementAt(i).Value.NewInstance(), allActiveActions.ElementAt(i).Key);
            }
        }
    }


    /// <summary>
    /// Pushes new actions to the <see cref="allActionsList"/>
    /// </summary>
    /// <param name="action">The action and all of its specific values which are needed for the history</param>
    private void Push(Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> action)
    {
        allActionsList.Add(action);
    }

    /// <summary>
    /// Finds the last executed action of a specific player.
    /// </summary>
    /// <param name="playerID">The player that wants to perform an undo/redo</param>
    /// <param name="type">the type of action he wants to perform</param>
    /// <returns>A tuple of the latest users action and if any later done action blocks the undo (True if some action is blocking || false if not)</returns>  
    /// Returns as second in the tuple that so each action could check it on its own >> List<ReversibleAction>>
    private Tuple<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>, bool> FindLastActionOfPlayer(string playerID, HistoryType type)
    {
        Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> result = null;

        for (int i = allActionsList.Count - 1; i > 0; i--)
        {
            if ((type == HistoryType.undoneAction && allActionsList[i].Item3 == HistoryType.undoneAction)
                || (type == HistoryType.action && allActionsList[i].Item3 == HistoryType.action)
                && allActionsList[i].Item2 == playerID)
            {
                result = allActionsList[i];
                break;
            }
        }
        if (result == null)
        {
            // Should´nt be reached.
            result = allActionsList[0];
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
        return new Tuple<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>, bool>(result, false);
    }

    /// <summary>
    /// Deletes all redos of a user
    /// </summary>
    /// <param name="userid">the user that does the new action</param>
    private void DeleteAllRedos(string userid)
    {
        for (int i = 0; i < allActionsList.Count; i++)
        {
            if (allActionsList[i].Item2.Equals(userid) && allActionsList[i].Item3.Equals(HistoryType.undoneAction))
            {
                allActionsList.RemoveAt(i);
                i--;
            }
            isRedo = false;
        }
    }


    /// <summary>
    /// Deletes an Item from the Action list depending on Time and Userid
    /// </summary>
    /// <param name="userid">the user for the action that should be deleted</param>
    /// <param name="id">the id of the action which should be deleted</param>
    private void DeleteItem(string userid, string id)
    {
        for (int i = 0; i < allActionsList.Count; i++)
        {
            if (allActionsList[i].Item6.Equals(id) && allActionsList[i].Item2.Equals(userid))
            {
                allActionsList.RemoveAt(i);
                return;
            }
        }
    }


    /// <summary>
    /// Undoes the last action with an effect of a specific player.
    /// </summary>
    /// <param name="userid"></param>
    public void Undo(string userid) // Fixme: undo and redo needs to update the active action, too.
    {
        int i = 0;
        foreach (Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> test in allActionsList)
        {
            i++;
            Debug.Log(i + test.Item4.ToString() + test.Item3 + "  " +  test.Item6);
        }
        Tuple<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>, bool> lastAction = FindLastActionOfPlayer(userid, HistoryType.action);

        while (!GetActiveAction(userid).HadEffect())
        {
            GetActiveAction(userid).Stop();
            if (allActionsList.Count > 1)
            {
                DeleteItem(userid, lastAction.Item1.Item6);
                lastAction = FindLastActionOfPlayer(userid, HistoryType.action);
                SetActiveAction(userid, lastAction.Item1.Item4);
            }
            else
            {
                // Fixme: What should we do, if undo is not possible?
                return;
            }
        }
        GetActiveAction(userid).Stop();
        GetActiveAction(userid).Undo();
        DeleteItem(userid, lastAction.Item1.Item6);
        Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> undoneAction = new Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>
            (DateTime.Now, userid, HistoryType.undoneAction, lastAction.Item1.Item4, lastAction.Item1.Item5, lastAction.Item1.Item4.Id());
        Push(undoneAction);
        lastAction = FindLastActionOfPlayer(userid, HistoryType.action);

        SetActiveAction(userid, lastAction.Item1.Item4);
        GetActiveAction(userid)?.Start();
    }

    /// <summary>
    /// Redoes the last undone action of a specific player.
    /// </summary>
    /// <param name="userid">The player who wants to execute the redo </param>
    public void Redo(string userid) // Fixme: undo and redo needs to update the active action, too.
    {
        GetActiveAction(userid)?.Stop();

        Tuple<Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>, bool> lastUndoneAction = FindLastActionOfPlayer(userid, HistoryType.undoneAction);

        lastUndoneAction.Item1.Item4.Redo();
        Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string> redoneAction = new Tuple<DateTime, string, HistoryType, ReversibleAction, List<string>, string>(DateTime.Now, userid, HistoryType.action, lastUndoneAction.Item1.Item4, lastUndoneAction.Item1.Item5, lastUndoneAction.Item1.Item4.Id());
        Push(redoneAction);
        lastUndoneAction.Item1.Item4.Start();

        SetActiveAction(userid, lastUndoneAction.Item1.Item4);
        DeleteItem(userid, lastUndoneAction.Item1.Item6);
    }

    /// <summary>
    /// Returns the Active Action for the Player
    /// </summary>
    /// <param name="player">The Player that performs an Action</param>
    /// <returns>The active action || null if key not in dictionary</returns>
    public ReversibleAction GetActiveAction(string player)
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
    }

}
