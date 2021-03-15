using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalActionHistory
{
    /// <summary>
    /// An enum which sets the type of an Action in the History
    /// </summary>
    public enum historyType
    {
        action,
        undo,
        redo,
    };

    /// <summary>
    /// the start of the ringbuffer
    /// </summary>
    private int head = 0;

    /// <summary>
    /// The end of the Ringbuffer
    /// </summary>
    private int tail = 0;

    /// <summary>
    /// The size of the ActionList
    /// </summary>
    private int size = 0;

    /// <summary>
    /// indicates wether the tail should move or not
    /// </summary>
    private bool full = false;

    /// <summary>
    /// The actionList it has an Tupel of the time as it was performed, Player ID, The type of the Action (Undo Redo Action), the ReversibleAction, and the list with the ids of the GameObjects
    /// A ringbuffer
    /// </summary>
    private Tuple<DateTime, string, historyType, ReversibleAction, List<string>>[] actionList; //FIXME: GameObject ID muss eine LISTE sein

    /// <summary>
    /// Constructs a new Global Action History and sets the Size of the Shared ActionList
    /// </summary>
    /// <param name="bufferSize">The Size of the shared ActionList Standard is 100</param>
    GlobalActionHistory(int bufferSize = 100)
    {
        actionList = new Tuple<DateTime, string, historyType, ReversibleAction, List<string>>[bufferSize];
        this.size = bufferSize;
    }


    /// <summary>
    /// Appends data to the Ringbuffer
    /// </summary>
    /// <param name="data"></param>
    private void Push(Tuple<DateTime, string, historyType, ReversibleAction, List<string>> data)
    {
        actionList[head] = data;
        if (head < size - 1) head++;
        else
        {
            head = 0;
            full = true;
        }
        if (full && tail < size - 1) tail++;
        else tail = 0;
    }

    /// <summary>
    /// Finds the latest player action of the searched type an all relevant newer changes
    /// </summary>
    /// <param name="playerID">The player that wants to perform an undo/redo</param>
    /// <param name="type">the type of action he wants to perform</param>
    /// <returns>A tuple of the latest users action and the actions from the others that be influenced by that</returns> 
    private Tuple<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>, List<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>>> Find(string playerID, historyType type)
    {
        int i = head - 1;

        //A list to persit all changes on the same GameObject
        List<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>> results = new List<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>>();
        Tuple<DateTime, string, historyType, ReversibleAction, List<string>> result = null;
        while (true)//FIXME: later difference whether it is an undo or redo search
        {

            if ((type == historyType.undo && actionList[i].Item3 != historyType.undo)
                || (type == historyType.redo && actionList[i].Item3 == historyType.undo)
                && actionList[i].Item2 == playerID)
            {
                result = actionList[i]; //FIXME: Somehow these data has to be deleted from the list, but not sure then to do it 
                break;
            }


            if (i == tail) break;
            if (i > 0) i--;
            else i = size - 1;
        }
        //Find all newer changes that could be a problem to the undo
        if (result != null) //IF Result == NULL no undo could be performed
        {
            while (true)
            {
                //Checks if any item from list 1 is in list 2
                if (result.Item5.Where(it => actionList[i].Item5.Contains(it)) != null) //FIXME: Could make some trouble, not sure if it works
                {
                    results.Add(actionList[i]);
                }
                if (i == head - 1) break;
                if (i < size - 1) i++;
                else i = 0;
            }
        }

        return new Tuple<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>, List<Tuple<DateTime, string, historyType, ReversibleAction, List<string>>>>(result,results);

    }
}
