using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.ActionHelpers;
using SEE.UI.PropertyDialog.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;
using SEE.Utils.History;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages the creation of a holistic metrics widget. It is needed so we can also revert the deletion.
    /// </summary>
    internal class AddWidgetAction : AbstractPlayerAction
    {
        /// <summary>
        /// This field can hold a reference to the dialog that the player will see in the process of executing this
        /// action.
        /// </summary>
        private AddWidgetDialog addWidgetDialog;

        /// <summary>
        /// Indicates how far this instance has progressed in adding a widget.
        /// </summary>
        private ProgressState progress = ProgressState.GetPosition;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Represents the different stages of progress of this action.
        /// </summary>
        private enum ProgressState
        {
            GetPosition,
            GetConfig,
            Finished
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat an <see cref="AddWidgetAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the board on which to create the widget.
            /// </summary>
            internal readonly string BoardName;

            /// <summary>
            /// The configuration of the widget that knows how the widget should be created.
            /// </summary>
            internal readonly WidgetConfig Config;

            /// <summary>
            /// Assigns the configuration of the widget to create and the name of the board on which to create it to
            /// fields of this class.
            /// </summary>
            /// <param name="boardName">The name of the board on which to create the widget.</param>
            /// <param name="config">The configuration; this is how the widget will be configured.</param>
            internal Memento(string boardName, WidgetConfig config)
            {
                this.BoardName = boardName;
                this.Config = config;
            }
        }

        /// <summary>
        /// Adds WidgetAdder components to all boards.
        /// </summary>
        public override void Start()
        {
            BoardsManager.ToggleWidgetAdders(true);
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.AddWidget"/>.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.GetPosition:
                    if (BoardsManager.TryGetWidgetAdditionPosition(out string boardName, out Vector3 position))
                    {
                        WidgetConfig config = new() { Position = position, ID = Guid.NewGuid() };
                        memento = new Memento(boardName, config);
                        addWidgetDialog = new AddWidgetDialog();
                        addWidgetDialog.Open();
                        progress = ProgressState.GetConfig;
                    }

                    return false;
                case ProgressState.GetConfig:
                    if (addWidgetDialog.WasCanceled())
                    {
                        // In case the player cancels the dialog
                        progress = ProgressState.GetPosition;
                        return false;
                    }
                    if (addWidgetDialog.TryGetConfig(out string metric, out string widget))
                    {
                        memento.Config.MetricType = metric;
                        memento.Config.WidgetName = widget;

                        WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);
                        if (widgetsManager != null)
                        {
                            widgetsManager.Create(memento.Config);
                            new CreateWidgetNetAction(memento.BoardName, memento.Config).Execute();
                        }
                        else
                        {
                            Debug.LogError(
                                $"No board found with the name {memento.BoardName} for adding the widget.\n");
                        }

                        progress = ProgressState.Finished;
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;
                    }

                    return false;
                case ProgressState.Finished:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Removes the <see cref="WidgetAdder"/>s from the metrics boards.
        /// </summary>
        public override void Stop()
        {
            BoardsManager.ToggleWidgetAdders(false);
        }

        /// <summary>
        /// Deletes the widget from the board on all clients.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);
            if (widgetsManager != null)
            {
                widgetsManager.Delete(memento.Config.ID);
                new DeleteWidgetNetAction(memento.BoardName, memento.Config.ID).Execute();
            }
            else
            {
                Debug.LogError($"No board found with the name {memento.BoardName} for deleting the widget.\n");
            }
        }

        /// <summary>
        /// Creates the new widget as configured, on all clients.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            WidgetsManager widgetsManager = BoardsManager.Find(memento.BoardName);
            if (widgetsManager != null)
            {
                widgetsManager.Create(memento.Config);
                new CreateWidgetNetAction(memento.BoardName, memento.Config).Execute();
            }
            else
            {
                Debug.LogError($"No board found with the name {memento.BoardName} for adding the widget.\n");
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddWidgetAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AddWidgetAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddWidgetAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the ID of the new widget and the name of the board which it was added onto.
        /// </summary>
        /// <returns>A HashSet of two items: The ID of the added widget and the name of the board it was added onto.
        /// </returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.BoardName, memento.Config.ID.ToString() };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.AddWidget"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.AddWidget;
        }
    }
}
