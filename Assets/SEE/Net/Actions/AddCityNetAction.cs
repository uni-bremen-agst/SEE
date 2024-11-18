using SEE.Game.City;
using SEE.Game;
using SEE.GameObjects;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Adds a City to all clients.
    /// </summary>
    public class AddCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table to which the city should be added.
        /// </summary>
        public string TableID;

        /// <summary>
        /// The city type that should be added.
        /// </summary>
        public CityTypes CityType;

        /// <summary>
        /// Creates a new AddCityNetAction.
        /// </summary>
        /// <param name="tableID">the unique name of the table to which the city should be added.</param>
        /// <param name="cityType">The city type that should be added.</param>
        public AddCityNetAction(string tableID, CityTypes cityType) : base()
        {
            TableID = tableID;
            CityType = cityType;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Adds the city of type <see cref="CityType"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject city = CitiesHolder.Find(TableID);
            city.GetComponent<CitySelectionManager>().CreateCity(CityType);
        }
    }
}