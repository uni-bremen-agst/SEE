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
        /// </summary>
        private enum Mode
        {
            init,
            push,
            delete,
            replace,
        };

        /// <summary>
        /// The specific instance of <see cref="Mode"/>
        /// </summary>
        private Mode mode = Mode.init;

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
            UnityEngine.Debug.LogWarning("Ausführung im Netzwerk");
            if (!IsRequester())
            {
                if (mode == Mode.push)
                {
                    GlobalActionHistory.Push(new Tuple<bool, ActionHistory.HistoryType, string, List<string>>(false, type, actionId, changedObjects));
                    UnityEngine.Debug.LogWarning("PUSSSH");
                }
                else if (mode == Mode.delete)
                {
                    GlobalActionHistory.DeleteItem(actionId, false);
                }
                else if (mode == Mode.replace)
                {
                    GlobalActionHistory.Replace(oldItem, newItem, true);
                }
                mode = Mode.init;
            }
        }

        /// <summary>
        /// Initiates the push of an action on each client
        /// </summary>
        /// <param name="type">Which type is the action (action, undoneAction)</param>
        /// <param name="actionId">The id of the action</param>
        /// <param name="changedObjects">The ids of the objects which are edited from the action</param>
        public void Push( ActionHistory.HistoryType type, string actionId, string changedObjects)
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
        /// <param name="actionId">The id of the action</param>
        public void Delete(string actionId) 
        {
            this.actionId = actionId;
            mode = Mode.delete;

            Execute(null);
        }

        /// <summary>
        /// Updates an entry through all clients.
        /// </summary>
        public void Replace(ActionHistory.HistoryType oldType, string id, string oldChangedObjects, ActionHistory.HistoryType newType, string newChangedObjects)
        {
            oldItem = new Tuple<bool, HistoryType, string, List<string>>(false, oldType, id, StringToList(oldChangedObjects));
            newItem = new Tuple<bool, HistoryType, string, List<string>>(false, newType, id, StringToList(newChangedObjects));
            mode = Mode.replace;
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