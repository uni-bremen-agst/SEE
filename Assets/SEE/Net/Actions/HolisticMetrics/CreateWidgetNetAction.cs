using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    public class CreateWidgetNetAction : AbstractNetAction
    {
        /// <summary>
        /// The name of the board on which to create the new widget.
        /// </summary>
        public string BoardName;

        /// <summary>
        /// The configuration of the new widget.
        /// </summary>
        public WidgetConfiguration WidgetConfiguration;

        /// <summary>
        /// The constructor of this class. It only assigns the parameter values to fields.
        /// </summary>
        /// <param name="boardName">The name of the board on which to create the new widget</param>
        /// <param name="widgetConfiguration">The configuration of the new widget</param>
        public CreateWidgetNetAction(string boardName, WidgetConfiguration widgetConfiguration)
        {
            BoardName = boardName;
            WidgetConfiguration = widgetConfiguration;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method executes the action on all clients, i.e., adds the widget on all clients.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            WidgetsManager widgetsManager = BoardsManager.GetWidgetsManager(BoardName);
            if (widgetsManager != null)
            {
                widgetsManager.Create(WidgetConfiguration);
            }
            else
            {
                Debug.LogError("No board found with the given name for adding the widget.");
            }
        }
    }
}