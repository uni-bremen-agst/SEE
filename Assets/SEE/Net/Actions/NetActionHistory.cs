using SEE.Utils;
using SEE.Controls.Actions;
using System.Collections.Generic;
using static SEE.Utils.ActionHistory;
using System.Linq;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Syncs the action history through the network on each client.
    /// </summary>
    public class NetActionHistory : AbstractNetAction
    {
        /// <summary>
        /// The state that determines which action should be performed.
        /// </summary>
        public enum ModeKind
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
        /// The specific instance of <see cref="ModeKind"/>
        /// </summary>
        public ModeKind Mode = ModeKind.Init;

        /// <summary>
        /// The type of the action (action or undoneAction).
        /// </summary>
        public ActionHistory.HistoryType Type;

        /// <summary>
        /// The ID of the action.
        /// </summary>
        public string ActionId;

        /// <summary>
        /// The IDs of all objects which are changed by the action.
        /// The data structure is a list, although logically it is a set.
        /// Yet, Unity cannot serialize HashSets, so we use List instead.
        /// </summary>
        public List<string> NewChangedObjects;

        /// <summary>
        /// The IDs of all objects which are changed by the old action.
        /// The data structure is a list, although logically it is a set.
        /// Yet, Unity cannot serialize HashSets, so we use List instead.
        /// </summary>
        public List<string> OldChangedObjects;

        /// <summary>
        /// The old items ID which has to be replaced.
        /// </summary>
        public string ID;

        /// <summary>
        /// The old items type which has to be replaced.
        /// </summary>
        public HistoryType OldItemType;

        /// <summary>
        /// The new items type which is replacing the old.
        /// </summary>
        public HistoryType NewItemType;

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
                switch(Mode){
                    case ModeKind.Push:
                        GlobalActionHistory.Push(new GlobalHistoryEntry(false, Type, ActionId, new HashSet<string>(NewChangedObjects)));
                        break;
                    case ModeKind.Delete:
                        GlobalActionHistory.RemoveAction(ActionId);
                        break;
                    case ModeKind.Replace:
                        GlobalActionHistory.Replace(new GlobalHistoryEntry(false, OldItemType, ID, new HashSet<string>(OldChangedObjects)),
                                                    new GlobalHistoryEntry(false, NewItemType, ID, new HashSet<string>(NewChangedObjects)),
                                                    true);
                        break;
                }
                Mode = ModeKind.Init;
            }
        }

        /// <summary>
        /// Initiates the push of this action on every client.
        /// </summary>
        /// <param name="type">The type of the action (action, undoneAction)</param>
        /// <param name="actionId">The ID of the action</param>
        /// <param name="changedObjects">The IDs of the objects edited by the action</param>
        public void Push(ActionHistory.HistoryType type, string actionId, HashSet<string> changedObjects)
        {
            Mode = ModeKind.Push;
            Type = type;
            ActionId = actionId;
            NewChangedObjects = changedObjects.ToList();
            Execute(null);
        }

        /// <summary>
        /// Initiates the deletion process on each client.
        /// </summary>
        /// <param name="actionId">The ID of the action</param>
        public void Delete(string actionId)
        {
            ActionId = actionId;
            Mode = ModeKind.Delete;
            Execute(null);
        }

        /// <summary>
        /// Updates an entry through all clients.
        /// </summary>
        /// <param name="id">The ID of the items.</param>
        /// <param name="oldType">The type of the old item.</param>
        /// <param name="oldChangedObjects">The changedObjects from the old item.</param>
        /// <param name="newType">The type of the new item.</param>
        /// <param name="newChangedObjects">The changedObjects of the new item.</param>
        public void Replace
                     (string id,
                      ActionHistory.HistoryType oldType,
                      HashSet<string> oldChangedObjects,
                      ActionHistory.HistoryType newType,
                      HashSet<string> newChangedObjects)
        {
            OldItemType = oldType;
            NewItemType = newType;
            ID = id;
            OldChangedObjects = oldChangedObjects.ToList();
            NewChangedObjects = newChangedObjects.ToList();
            Mode = ModeKind.Replace;
            Execute(null);
        }
    }
}