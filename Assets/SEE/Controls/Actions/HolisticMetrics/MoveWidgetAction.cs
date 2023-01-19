using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages a move action of a metrics widget from its old position to a new position where the player
    /// dragged it.
    /// </summary>
    internal class MoveWidgetAction : AbstractPlayerAction
    {
        private Memento memento;

        private struct Memento
        {
            /// <summary>
            /// The name of the board by which we identify the board on which the widget to be moved should be.
            /// </summary>
            public readonly string boardName;

            /// <summary>
            /// The ID of the widget to be moved, so we can identify it.
            /// </summary>
            public readonly Guid widgetID;

            /// <summary>
            /// The old position of the widget, so we can reset it to that position if needed.
            /// </summary>
            public readonly Vector3 oldPosition;

            /// <summary>
            /// The new position of the widget to which the widget will be moved.
            /// </summary>
            public readonly Vector3 newPosition;

            public Memento(string boardName, Guid widgetID, Vector3 oldPosition, Vector3 newPosition)
            {
                this.boardName = boardName;
                this.widgetID = widgetID;
                this.oldPosition = oldPosition;
                this.newPosition = newPosition;
            }
        }

        public override void Start()
        {
            BoardsManager.ToggleWidgetsMoving();
        }

        public override bool Update()
        {
            if (BoardsManager.GetWidgetMovement(
                    out Vector3 oldPosition,
                    out Vector3 newPosition,
                    out string boardName,
                    out Guid widgetID))
            {
                memento = new Memento(boardName, widgetID, oldPosition, newPosition);
                Redo();
                return true;
            }

            return false;
        }

        public override void Stop()
        {
            BoardsManager.ToggleWidgetsMoving();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MoveWidgetAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Moves the widget to the old position on all clients.
        /// </summary>
        public override void Undo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(memento.boardName);
            if (widgetsManager != null)
            {
                widgetsManager.Move(memento.widgetID, memento.oldPosition);    
                new MoveWidgetNetAction(memento.boardName, memento.widgetID, memento.oldPosition).Execute();
            }
            else
            {
                Debug.LogError($"Could not find the board {memento.boardName} for moving a widget on it.\n");
            }
        }

        /// <summary>
        /// Moves the widget to the new position on all clients.
        /// </summary>
        public override void Redo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(memento.boardName);
            if (widgetsManager != null)
            {
                widgetsManager.Move(memento.widgetID, memento.newPosition);
                new MoveWidgetNetAction(memento.boardName, memento.widgetID, memento.newPosition).Execute();
            }
            else
            {
                Debug.LogError($"Could not find the board {memento.boardName} for moving a widget on it.\n");
            }
        }
        
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.widgetID.ToString() };
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.MoveWidget;
        }
    }
}