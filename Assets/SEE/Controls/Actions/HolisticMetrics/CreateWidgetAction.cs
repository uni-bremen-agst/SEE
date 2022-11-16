using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class manages the creation of a holistic metrics widget. It is needed so we can also revert the deletion.
    /// </summary>
    internal class CreateWidgetAction : Action
    {
        /// <summary>
        /// The name of the board on which to create the widget.
        /// </summary>
        private readonly string boardName;

        /// <summary>
        /// The configuration of the widget that knows how the widget should be created.
        /// </summary>
        private readonly WidgetConfig config;

        /// <summary>
        /// Assigns the configuration of the widget to create and the name of the board on which to create it to fields
        /// of this class.
        /// </summary>
        /// <param name="boardName">The name of the board on which to create the widget</param>
        /// <param name="config">The configuration; this is how the widget will be configured</param>
        internal CreateWidgetAction(string boardName, WidgetConfig config)
        {
            this.boardName = boardName;
            this.config = config;
        }
        
        /// <summary>
        /// Creates the new widget as configured, on all clients.
        /// </summary>
        internal override void Do()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            if (widgetsManager != null)
            {
                widgetsManager.Create(config);
                new CreateWidgetNetAction(boardName, config).Execute();
            }
            else
            {
                Debug.LogError($"No board found with the name {boardName} for adding the widget.\n");
            }
        }

        /// <summary>
        /// Deletes the widget from the board on all clients.
        /// </summary>
        internal override void Undo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);
            if (widgetsManager != null)
            {
                widgetsManager.Delete(config.ID);
                new DeleteWidgetNetAction(boardName, config.ID).Execute();
            }
            else
            {
                Debug.LogError($"No board found with the name {boardName} for deleting the widget.\n");
            }
        }
    }
}