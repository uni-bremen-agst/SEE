using System;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages a move action of a metrics widget from its old position to a new position where the player
    /// dragged it.
    /// </summary>
    internal class MoveWidgetAction : Action
    {
        /// <summary>
        /// The name of the board by which we identify the board on which the widget to be moved should be.
        /// </summary>
        private readonly string boardName;

        /// <summary>
        /// The ID of the widget to be moved, so we can identify it.
        /// </summary>
        private readonly Guid widgetID;

        /// <summary>
        /// The old position of the widget, so we can reset it to that position if needed.
        /// </summary>
        private readonly Vector3 oldPosition;

        /// <summary>
        /// The new position of the widget to which the widget will be moved.
        /// </summary>
        private readonly Vector3 newPosition;

        /// <summary>
        /// Sets the fields of this class to the given parameter values.
        /// </summary>
        /// <param name="boardName">The name of the board on which to move a widget</param>
        /// <param name="widgetID">The ID of the widget that should be moved</param>
        /// <param name="oldPosition">The position of the widget before this moving action</param>
        /// <param name="newPosition">The position to which the widget should be moved</param>
        internal MoveWidgetAction(string boardName, Guid widgetID, Vector3 oldPosition, Vector3 newPosition)
        {
            this.boardName = boardName;
            this.widgetID = widgetID;
            this.oldPosition = oldPosition;
            this.newPosition = newPosition;
        }
        
        /// <summary>
        /// Moves the widget to the new position on all clients.
        /// </summary>
        internal override void Do()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            if (widgetsManager != null)
            {
                widgetsManager.Move(widgetID, newPosition);
                new MoveWidgetNetAction(boardName, widgetID, newPosition).Execute();
            }
            else
            {
                Debug.LogError($"Could not find the board {boardName} for moving a widget on it.\n");
            }
        }

        /// <summary>
        /// Moves the widget to the old position on all clients.
        /// </summary>
        internal override void Undo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            widgetsManager.Move(widgetID, oldPosition);
            new MoveWidgetNetAction(boardName, widgetID, oldPosition).Execute();
        }
    }
}