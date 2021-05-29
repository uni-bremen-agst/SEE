using SEE.Utils;
using SEE.Controls.Actions;
using System.Collections.Generic;
using static SEE.Utils.ActionHistory;

namespace SEE.Net
{
    /// <summary>
    /// Syncs the action history through the network on each client.
    /// </summary>
    public class NetActionHistory : AbstractAction
    {
        /// <summary>
        /// The state that determines which action should be performed.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// The initial state, no code executedOnClient.
            /// </summary>
            Init,
            /// <summary>
            /// A new item should be pushed onto the globalHistory on each client.
            /// </summary>
            Push,
            /// <summary>
            /// An old item should be deleted from the globalHistory on each client.
            /// </summary>
            Delete,
            /// <summary>
            /// An entry in the globalHistory should be overwritten by newer changes on each client.
            /// </summary>
            Replace,
        };

        /// <summary>
        /// The specific instance of <see cref="Mode"/>
        /// </summary>
        public Mode mode = Mode.Init;

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
        /// The IDs of all objects which are changed by the old action.
        /// </summary>
        public List<string> oldChangedObjects;

        /// <summary>
        /// The old items ID which has to be replaced.
        /// </summary>
        public string ID;

        /// <summary>
        /// The old items type which has to be replaced.
        /// </summary>
        public HistoryType oldItemType;

        /// <summary>
        /// The new items type which is replacing the old.
        /// </summary>
        public HistoryType newItemType;

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
                switch(mode){
                    case Mode.Push:
                        GlobalActionHistory.Push(new GlobalHistoryEntry(false, type, actionId, changedObjects));
                        break;
                    case Mode.Delete:
                        GlobalActionHistory.DeleteItem(actionId);
                        break;
                    case Mode.Replace:
                        GlobalActionHistory.Replace(new GlobalHistoryEntry(false, oldItemType, ID, oldChangedObjects),
                                                    new GlobalHistoryEntry(false, newItemType, ID, changedObjects),
                                                    true);
                        break;
                }
                mode = Mode.Init;
            }
        }

        /// <summary>
        /// Initiates the push of an action on each client
        /// </summary>
        /// <param name="type">The type of the action (action, undoneAction)</param>
        /// <param name="actionId">The ID of the action</param>
        /// <param name="changedObjects">The IDs of the objects which are edited from the action</param>
        public void Push(ActionHistory.HistoryType type, string actionId, List<string> changedObjects)
        {
            mode = Mode.Push;
            this.type = type;
            this.actionId = actionId;
            this.changedObjects = changedObjects;
            Execute(null);
        }

        /// <summary>
        /// Initiates the deletion process on each client.
        /// </summary>
        /// <param name="actionId">The ID of the action</param>
        public void Delete(string actionId)
        {
            this.actionId = actionId;
            mode = Mode.Delete;
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
        public void Replace
                     (ActionHistory.HistoryType oldType, 
                      string id, 
                      List<string >oldChangedObjects, 
                      ActionHistory.HistoryType newType, 
                      List<string> newChangedObjects)
        {
            oldItemType = oldType;
            ID = id;
            this.oldChangedObjects = oldChangedObjects;
            this.changedObjects = newChangedObjects;
            mode = Mode.Replace;
            Execute(null);
        }
    }
}