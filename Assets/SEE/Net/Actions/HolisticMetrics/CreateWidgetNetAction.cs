using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for adding a widget on all clients.
    /// </summary>
    public class CreateWidgetNetAction : HolisticMetricsNetAction
    {
        /// <summary>
        /// The name of the board on which to create the new widget.
        /// </summary>
        public string BoardName;

        /// <summary>
        /// The configuration of the new widget.
        /// </summary>
        public WidgetConfig WidgetConfig;

        /// <summary>
        /// The constructor of this class. It only assigns the parameter values to fields.
        /// </summary>
        /// <param name="boardName">The name of the board on which to create the new widget</param>
        /// <param name="widgetConfig">The configuration of the new widget</param>
        public CreateWidgetNetAction(string boardName, WidgetConfig widgetConfig)
        {
            BoardName = boardName;
            WidgetConfig = widgetConfig;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method executes the action on all clients except the requester, i.e., adds the widget on all clients.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Find(BoardName).Create(WidgetConfig);
            }
            
        }
    }
}