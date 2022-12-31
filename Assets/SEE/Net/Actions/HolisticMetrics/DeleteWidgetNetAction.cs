using System;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for deleting a widget on all clients.
    /// </summary>
    public class DeleteWidgetNetAction : HolisticMetricsNetAction
    {
        /// <summary>
        /// The name of the board from which to delete the widget.
        /// </summary>
        public string BoardName;
        
        /// <summary>
        /// The ID of the widget to delete.
        /// </summary>
        public Guid WidgetID;
        
        /// <summary>
        /// The constructor of this class. This only assigns the parameter values to fields.
        /// </summary>
        /// <param name="boardName">The name of the board from which to delete the widget</param>
        /// <param name="widgetID">The ID of the widget to delete</param>
        public DeleteWidgetNetAction(string boardName, Guid widgetID)
        {
            BoardName = boardName;
            WidgetID = widgetID;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Executes the action on all clients except the requester, i.e., deletes the widget on all clients.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Find(BoardName).Delete(WidgetID);   
            }
        }
    }
}
