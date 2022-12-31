using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Net.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for sending a request to all clients to change the city selection on a metrics board.
    /// </summary>
    public class SwitchCityNetAction : HolisticMetricsNetAction
    {
        /// <summary>
        /// The name of the board on which to change the selection.
        /// </summary>
        public string BoardName;

        /// <summary>
        /// The name of the city to change the selection to.
        /// </summary>
        public string CityName;
        
        /// <summary>
        /// The constructor. This assigns the parameter values to fields.
        /// </summary>
        /// <param name="boardName">The name of the board on which to change the selection</param>
        /// <param name="cityName">The name of the city to select</param>
        public SwitchCityNetAction(string boardName, string cityName)
        {
            BoardName = boardName;
            CityName = cityName;
        }
        
        /// <summary>
        /// This method does nothing.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// This method executes the action on all clients except the requester which means it calls the method of the
        /// metric board with the given name that will change the selection.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Find(BoardName).SwitchCity(CityName);
            }
        }
    }
}