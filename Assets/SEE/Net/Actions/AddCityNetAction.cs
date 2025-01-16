using SEE.Game.City;
using SEE.GameObjects;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Adds a city to all clients.
    /// </summary>
    public class AddCityNetAction : CityNetAction
    {
        /// <summary>
        /// The city type that should be added.
        /// </summary>
        public CityTypes CityType;

        /// <summary>
        /// The name for the added city.
        /// </summary>
        public string CityName;

        /// <summary>
        /// The constructor of this action. Sets <see cref="CityType"/> to determine the type of city to be added
        /// and the <see cref="CityName"/> to name the city accordingly.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which the city should be added.</param>
        /// <param name="cityType">The city type that should be added.</param>
        /// <param name="cityName">The name to be assigned to the created city.</param>
        public AddCityNetAction(string tableID, CityTypes cityType, string cityName) : base(tableID)
        {
            CityType = cityType;
            CityName = cityName;
        }

        /// <summary>
        /// Adds the city of type <see cref="CityType"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            City.GetComponent<CitySelectionManager>().CreateCity(CityType, CityName);
        }
    }
}
