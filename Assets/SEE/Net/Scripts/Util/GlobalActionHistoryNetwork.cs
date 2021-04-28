using Assets.SEE.Utils;
using SEE.Controls.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Sync the actionhistory through the network on each client.
    /// </summary>
    public  class GlobalActionHistoryNetwork : AbstractAction
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly bool isOwner;
        /// <summary>
        /// 
        /// </summary>
        private ActionHistory.HistoryType type;
        /// <summary>
        /// 
        /// </summary>
        private readonly string actionId;
        /// <summary>
        /// 
        /// </summary>
        private List<string> changedObjects;

        /// <summary>
        /// Wether a object should be pushed or removed
        /// </summary>
        private bool push = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isOwner"></param>
        /// <param name="type"></param>
        /// <param name="actionId"></param>
        /// <param name="changedObjects"></param>
        public GlobalActionHistoryNetwork(bool isOwner, ActionHistory.HistoryType type, string actionId, string changedObjects, bool push)
        {
            this.isOwner = isOwner;
            this.type = type;
            this.actionId = actionId;
            this.changedObjects = StringToList(changedObjects);
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

                if (push) GlobalActionHistory.Push(new Tuple<bool, ActionHistory.HistoryType, string, List<string>>(!isOwner, type, actionId, changedObjects));
                else GlobalActionHistory.DeleteItem(actionId, false);
            }
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
                Debug.LogWarning("ChangedObjects are null \n");
                return null;
            }
            string[] changedObjectsAsArray = new string[changedObjectsToParse.Split(',').Length];
            changedObjectsAsArray = changedObjectsToParse.Split(',');
            List<string> result = changedObjectsAsArray.ToList();
            return result;
        }
    }
}