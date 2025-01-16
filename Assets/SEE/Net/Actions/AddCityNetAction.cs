using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using System;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Adds a city to all clients.
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
        /// The name for the added city.
        /// </summary>
        public string CityName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which the city should be added.</param>
        /// <param name="cityType">The city type that should be added.</param>
        /// <param name="cityName">The name to be assigned to the created city.</param>
        public AddCityNetAction(string tableID, CityTypes cityType, string cityName) : base()
        {
            TableID = tableID;
            CityType = cityType;
            CityName = cityName;
        }

        /// <summary>
        /// Adds the city of type <see cref="CityType"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject city = citiesHolder.Find(TableID);
                if (city == null)
                {
                    throw new Exception($"The city can't be found");
                }
                city.GetComponent<CitySelectionManager>().CreateCity(CityType, CityName);
            }
            else
            {
                throw new Exception($"The city can't be added because there is no CitieHolder component.");
            }
        }
    }
}
