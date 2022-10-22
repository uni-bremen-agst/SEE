using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for executing or reverting widget deletions. (holistic metric widgets)
    /// </summary>
    internal class DeleteWidgetAction : Action
    {
        /// <summary>
        /// The name of the board from which to delete the widget.
        /// </summary>
        private readonly string boardName;

        /// <summary>
        /// The configuration of the widget, so it can be restored.
        /// </summary>
        private readonly WidgetConfig widgetConfig;

        /// <summary>
        /// Writes the two parameter values into fields of the class.
        /// </summary>
        /// <param name="boardName">The name of the board from which to delete the widget</param>
        /// <param name="widgetConfig">The configuration of the widget, so it can be restored</param>
        internal DeleteWidgetAction(string boardName, WidgetConfig widgetConfig)
        {
            this.boardName = boardName;
            this.widgetConfig = widgetConfig;
        }
        
        /// <summary>
        /// This method executes the action (deletes the widget from the board on all clients).
        /// </summary>
        internal override void Do()
        {
            // The widgets manager that manages the widget we want to delete
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);

            if (widgetsManager != null)
            {
                // Delete the widget locally
                widgetsManager.Delete(widgetConfig.ID);
            
                // Delete the widget on the other clients
                new DeleteWidgetNetAction(boardName, widgetConfig.ID).Execute();   
            }
            else
            {
                Debug.LogError("Tried to delete a widget from a board that could not be found.");
            }
        }

        /// <summary>
        /// This method creates the widget again from the saved configuration (on all clients).
        /// </summary>
        internal override void Undo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(boardName);

            if (widgetsManager != null)
            {
                widgetsManager.Create(widgetConfig);
                new CreateWidgetNetAction(boardName, widgetConfig).Execute();    
            }
            else
            {
                Debug.LogError("Tried to create a widget on a board that could not be found.");
            }
        }
    }
}