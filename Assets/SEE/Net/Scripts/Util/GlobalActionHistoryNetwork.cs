using Assets.SEE.Utils;
using SEE.Controls.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Assets.SEE.Utils.ActionHistory;

namespace SEE.Net
{
    /// <summary>
    /// Sync the actionhistory through the network on each client.
    /// </summary>
    public  class GlobalActionHistoryNetwork : AbstractAction
    { 
        /// <summary>
        /// The type of the action (action or undoneAction).
        /// </summary>
        public ActionHistory.HistoryType type;

        /// <summary>
        /// The id of the action.
        /// </summary>
        public string actionId;

        /// <summary>
        /// The ids of all objects which are changed by the action.
        /// </summary>
        public List<string> changedObjects;

        /// <summary>
        /// True, if an action should be pushed, else false.
        /// </summary>
        public bool push = false;

        /// <summary>
        /// True, if an action should be deleted, else false.
        /// </summary>
        public bool delete = false;


        /// <summary>
        /// True, if an action should be replaced, else false.
        /// </summary>
        public bool replace = false;

        /// <summary>
        /// The old item which has to be replaced.
        /// </summary>
        public Tuple<bool, HistoryType, string, List<string>> oldItem;

        /// <summary>
        /// The new item which is replacing the old.
        /// </summary>
        public Tuple<bool, HistoryType, string, List<string>> newItem;

        public GlobalActionHistoryNetwork() 
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Stuff to execute on the Server. Nothing to be done here.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Syncs the GlobalActionHistory on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (push) GlobalActionHistory.Push(new Tuple<bool, ActionHistory.HistoryType, string, List<string>>(false, type, actionId, changedObjects));
                else if (delete) GlobalActionHistory.DeleteItem(actionId, false);
                else if (replace) GlobalActionHistory.Replace(oldItem, newItem, true);
                else UnityEngine.Debug.Log("Sollte nicht erreicht werden");
            }
        }

        /// <summary>
        /// Initiats the push of an action on each client
        /// </summary>
        /// <param name="type">Which type is the action (action, undoneAction)</param>
        /// <param name="actionId">The id of the action</param>
        /// <param name="changedObjects">The ids of the objects which are edited from the action</param>
        public void Push( ActionHistory.HistoryType type, string actionId, string changedObjects)
        { 
            push = true;
            this.type = type;
            this.actionId = actionId;
            this.changedObjects = StringToList(changedObjects);
            Execute(null);    
        }

        /// <summary>
        /// Initiats the Deletion process on each Client
        /// </summary>
        /// <param name="actionId">The id of the action</param>
        public void Delete(string actionId) 
        {
            this.actionId = actionId;
            push = false;
            Execute(null);
        }

        /// <summary>
        /// Updates an entry through all clients
        /// </summary>
        public void Replace(ActionHistory.HistoryType oldType, string id, string oldChangedObjects, ActionHistory.HistoryType newType, string newChangedObjects)
        {
            oldItem = new Tuple<bool, HistoryType, string, List<string>>(false, oldType, id, StringToList(oldChangedObjects));
            newItem = new Tuple<bool, HistoryType, string, List<string>>(false, newType, id, StringToList(newChangedObjects));
            push = false;
            delete = false;
            replace = true;
            Execute(null);
        }

        /// <summary>
        /// Parses a comma-seperated string of changedObjects and returns them as single elements in a list.
        /// </summary>
        /// <param name="changedObjectsToParse">the changed objects which has to be parsed</param>
        /// <returns>a list of names of changed gameObjects</returns>
        private List<string> StringToList(string changedObjectsToParse)
        {
            if (changedObjectsToParse == null)
            {
                return null;
            }
            string[] changedObjectsAsArray = new string[changedObjectsToParse.Split(',').Length];
            changedObjectsAsArray = changedObjectsToParse.Split(',');
            List<string> result = changedObjectsAsArray.ToList();
            return result;
        }
    }
}