using Assets.SEE.Game;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.GO.Menu;
using System;
using System.Collections.Generic;
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
        private bool isOwner;
        /// <summary>
        /// 
        /// </summary>
        private GlobalActionHistory.HistoryType type;
        /// <summary>
        /// 
        /// </summary>
        private string actionId;
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
        public GlobalActionHistoryNetwork(bool isOwner, GlobalActionHistory.HistoryType type, string actionId, List<string> changedObjects, bool push)
        {
            this.isOwner = isOwner;
            this.type = type;
            this.actionId = actionId;
            this.changedObjects = changedObjects;
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

                if (push) PlayerActionHistory.history.Push(new Tuple<bool, GlobalActionHistory.HistoryType, string, List<string>>(!isOwner, type, actionId, changedObjects));
                else PlayerActionHistory.history.DeleteItem(actionId, false);

            }
        }
    }
}