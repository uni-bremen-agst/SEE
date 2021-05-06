using Assets.SEE.Utils;
using SEE.Controls.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets.SEE.Utils.ActionHistory;

namespace SEE.Net
{
    /// <summary>
    /// Sync the actionhistory through the network on each client.
    /// </summary>
    public  class GlobalActionHistoryNetwork : AbstractAction
    { 
        /// <summary>
        /// which type of action (action, undoneAction) 
        /// </summary>
        public ActionHistory.HistoryType type;

        /// <summary>
        /// The id of the action
        /// </summary>
        public string actionId;

        /// <summary>
        /// all ids from the objects that the action has changed
        /// </summary>
        public List<string> changedObjects;

        /// <summary>
        /// Wether a object should be pushed
        /// </summary>
        public bool push = false;

        /// <summary>
        /// Wether a object should be deleted
        /// </summary>
        public bool delete = false;

        /// <summary>
        /// Syncs the ActionHistory between the Clients
        /// </summary>
        /// 

        Tuple<bool, HistoryType, string, List<string>> oldItem;

        Tuple<bool, HistoryType, string, List<string>> newItem;
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
                else GlobalActionHistory.Replace(oldItem, newItem, true);
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
        /// Updates an Entry through all clients
        /// </summary>
        public void Replace(ActionHistory.HistoryType oldType, string id, string oldChangedObjects, ActionHistory.HistoryType newType, string newChangedObjects)
        {
            oldItem = new Tuple<bool, HistoryType, string, List<string>>(false, oldType, id, StringToList(oldChangedObjects));
            newItem = new Tuple<bool, HistoryType, string, List<string>>(false, newType, id, StringToList(newChangedObjects));
            push = false;
            delete = false;
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