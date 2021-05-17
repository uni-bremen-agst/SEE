using Assets.SEE.Utils;
using SEE.Controls.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Assets.SEE.Utils.ActionHistory;

namespace SEE.Net
{
    /// <summary>
    /// Syncs the action history through the network on each client.
    /// </summary>
    public  class GlobalActionHistoryNetwork : AbstractAction
    { 

        /// <summary>
        /// The state which determines which action should be performed.
        /// States:
        /// init: the initial state, no code executedOnClient.
        /// push: an new item should be pushed to the globalHistory on each client.
        /// delete: an old item should be deleted from the globalHistory on each client.
        /// replace: an entry in the globalHistory should be overwritten by newer changes on each client.
        /// </summary>
        public enum Mode
        {
            init,
            push,
            delete,
            replace,
        };

        /// <summary>
        /// The specific instance of <see cref="Mode"/>
        /// </summary>
        public Mode mode = Mode.init;

        /// <summary>
        /// The type of the action (action or undoneAction).
        /// </summary>
        public ActionHistory.HistoryType type;

        /// <summary>
        /// The ID of the action.
        /// </summary>
        public string actionId;

        /// <summary>
        /// The IDs of all objects which are changed by the action.
        /// </summary>
        public List<string> changedObjects;

        /// <summary>
        /// The old item which has to be replaced.
        /// </summary>
        public GlobalHistoryEntry oldItem;

        /// <summary>
        /// The new item which is replacing the old.
        /// </summary>
        public GlobalHistoryEntry newItem;

        public GlobalActionHistoryNetwork() 
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Stuff to execute on the server. Nothing to be done here.
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
                if (mode == Mode.push)
                {
                    GlobalActionHistory.Push(new GlobalHistoryEntry(false, type, actionId, changedObjects));
                }
                else if (mode == Mode.delete)
                {
                    GlobalActionHistory.DeleteItem(actionId);
                }
                else if (mode == Mode.replace)
                {
                    UnityEngine.Debug.LogError("NETWORK ID" + oldItem.ActionID + " " + newItem.ActionID);
                    GlobalActionHistory.Replace(oldItem, newItem, true);
                }
                mode = Mode.init;
            }
        }

        /// <summary>
        /// Initiates the push of an action on each client
        /// </summary>
        /// <param name="type">The type of the action (action, undoneAction)</param>
        /// <param name="actionId">The ID of the action</param>
        /// <param name="changedObjects">The IDs of the objects which are edited from the action</param>
        public void Push(ActionHistory.HistoryType type, string actionId, string changedObjects)
        {
            mode = Mode.push;
            this.type = type;
            this.actionId = actionId;
            this.changedObjects = StringToList(changedObjects);
            Execute(null);    

        }

        /// <summary>
        /// Initiates the deletion process on each client.
        /// </summary>
        /// <param name="actionId">The ID of the action</param>
        public void Delete(string actionId) 
        {
            this.actionId = actionId;
            mode = Mode.delete;

            Execute(null);
        }

        /// <summary>
        /// Updates an entry through all clients.
        /// </summary>
        /// <param name="oldType">The type of the old item.</param>
        /// <param name="id">The ID of the items.</param>
        /// <param name="oldChangedObjects">The changedObjects from the old item.</param>
        /// <param name="newType">The type of the new item.</param>
        /// <param name="newChangedObjects">The changedObjects of the new item.</param>
        public void Replace(ActionHistory.HistoryType oldType, string id, string oldChangedObjects, ActionHistory.HistoryType newType, string newChangedObjects)
        {
            oldItem = new GlobalHistoryEntry(false, oldType, id, StringToList(oldChangedObjects));
            newItem = new GlobalHistoryEntry(false, newType, id, StringToList(newChangedObjects));
            mode = Mode.replace;
            Execute(null);
        }

        /// <summary>
        /// Parses a comma-seperated string of changedObjects and returns them as single elements in a list.
        /// </summary>
        /// <param name="changedObjectsToParse">the changed objects which has to be parsed</param>
        /// <returns>a list of names of changed gameObjects</returns>
        private static List<string> StringToList(string changedObjectsToParse)
        {
            return changedObjectsToParse?.Split('?').ToList();
        }
    }
}