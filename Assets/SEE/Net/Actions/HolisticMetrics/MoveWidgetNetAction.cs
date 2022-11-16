using System;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class handles the execution of the MoveWidgetAction on all other clients except the requester.
    /// </summary>
    public class MoveWidgetNetAction : AbstractNetAction
    {
        public string BoardName;

        public Guid WidgetID;

        public Vector3 Position;
            
        /// <summary>
        /// Saves the parameter values in fields of this class.
        /// </summary>
        /// <param name="boardName">The name of the board on which the widget is found</param>
        /// <param name="widgetID">The ID of the widget to be moved</param>
        /// <param name="position">The position to which to move the metric</param>
        public MoveWidgetNetAction(string boardName, Guid widgetID, Vector3 position)
        {
            BoardName = boardName;
            WidgetID = widgetID;
            Position = position;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Moves the widget on all clients except the requester.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                WidgetsManager widgetsManager = BoardsManager.Find(BoardName);
                if (widgetsManager != null)
                {
                    widgetsManager.Move(WidgetID, Position);    
                }
                else
                {
                    Debug.LogError($"Tried to move a widget, but the correlating board named {BoardName} " +
                                   "was not found\n");
                }
            }
        }
    }
}