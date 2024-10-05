using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils.History;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for executing or reverting widget deletions. (holistic metric widgets)
    /// </summary>
    internal class DeleteWidgetAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="DeleteWidgetAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the board from which to delete the widget.
            /// </summary>
            internal readonly string BoardName;

            /// <summary>
            /// The configuration of the widget, so it can be restored.
            /// </summary>
            internal readonly WidgetConfig WidgetConfig;

            /// <summary>
            /// Writes the two parameter values into fields of the class.
            /// </summary>
            /// <param name="boardName">The name of the board from which to delete the widget</param>
            /// <param name="widgetConfig">The configuration of the widget, so it can be restored</param>
            internal Memento(string boardName, WidgetConfig widgetConfig)
            {
                this.BoardName = boardName;
                this.WidgetConfig = widgetConfig;
            }
        }

        /// <summary>
        /// Add components to the widgets that will listen for mouse clicks.
        /// </summary>
        public override void Start()
        {
            BoardsManager.ToggleWidgetDeleting(true);
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DeleteWidget"/>.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (BoardsManager.TryGetWidgetDeletion(out string boardName, out WidgetConfig widgetConfig))
            {
                memento = new Memento(boardName, widgetConfig);
                WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);

                if (widgetsManager != null)
                {
                    widgetsManager.Delete(memento.WidgetConfig.ID);
                    new DeleteWidgetNetAction(memento.BoardName, memento.WidgetConfig.ID).Execute();
                }
                else
                {
                    Debug.LogError($"Tried to delete a widget from a board named {memento.BoardName} that " +
                                   $"could not be found.\n");
                }

                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes components from widgets that would listen for mouse clicks on the widgets.
        /// </summary>
        public override void Stop()
        {
            BoardsManager.ToggleWidgetDeleting(false);
        }

        /// <summary>
        /// This method executes the action (deletes the widget from the board on all clients).
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);

            if (widgetsManager != null)
            {
                widgetsManager.Delete(memento.WidgetConfig.ID);
                new DeleteWidgetNetAction(memento.BoardName, memento.WidgetConfig.ID).Execute();
            }
            else
            {
                Debug.LogError($"Tried to delete a widget from a board named {memento.BoardName} that " +
                               $"could not be found.\n");
            }
        }

        /// <summary>
        /// This method creates the widget again from the saved configuration (on all clients).
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);

            if (widgetsManager != null)
            {
                widgetsManager.Create(memento.WidgetConfig);
                new CreateWidgetNetAction(memento.BoardName, memento.WidgetConfig).Execute();
            }
            else
            {
                Debug.LogError($"Tried to create a widget on a board named {memento.BoardName} that " +
                               $"could not be found.\n");
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteWidgetAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new DeleteWidgetAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteWidgetAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the ID of the widget that was deleted in this action.
        /// </summary>
        /// <returns>A HashSet of strings containing one item which is the ID of the widget that was moved</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.WidgetConfig.ID.ToString() };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.DeleteWidget"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.DeleteWidget;
        }
    }
}
