using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;

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
        /// The name for the added city.
        /// </summary>
        public string CityName;

        /// <summary>
        /// Creates a new AddCityNetAction.
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
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                GameObject city = citiesHolder.Find(TableID);
                city.GetComponent<CitySelectionManager>().CreateCity(CityType, CityName);
            }
            else
            {
                throw new Exception($"The city can't be added because there is no CitieHolder component.");
            }
        }
    }
}