using SEE.Game;
using SEE.GameObjects;
using System;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Superclass for all city net actions.
    /// </summary>
    public class CityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the table to interact with its city.
        /// </summary>
        public string TableID;

        /// <summary>
        /// The city object that should be manipulated by this action.
        /// Will be set in the <see cref="ExecuteOnClient"/> method.
        /// </summary>
        protected GameObject City {  get; private set; }

        /// <summary>
        /// The constructor of this action. It sets the <see cref="TableID"/> to identify the table whose
        /// city is to be interacted with.
        /// </summary>
        /// <param name="tableID">The unique name of the table to interact with its city.</param>
        public CityNetAction(string tableID) : base()
        {
            TableID = tableID;
        }

        /// <summary>
        /// Returns the city object with the id <see cref="TableID"/>
        /// </summary>
        /// <returns>the found city object (never null)</returns>
        /// <exception cref="Exception">thrown if a city object cannot be found</exception>
        private GameObject Find()
        {
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject city = citiesHolder.Find(TableID);
                if (city == null)
                {
                    throw new Exception($"The city can't be found on the table {TableID} ");
                }
                return city;
            }
            else
            {
                throw new Exception($"The city can't be found because there is no CitieHolder component.");
            }
        }

        /// <summary>
        /// Unifies the search for the city object for the subclasses.
        /// </summary>
        public override void ExecuteOnClient()
        {
            City = Find();
        }
    }
}
