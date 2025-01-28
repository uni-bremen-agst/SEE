namespace SEE.Net.Actions.City
{
    /// <summary>
    /// Superclass for all add city net actions.
    /// </summary>
    public abstract class AddCityNetAction : CityNetAction
    {
        /// <summary>
        /// The name for the added city.
        /// </summary>
        public string CityName;

        /// <summary>
        /// The constructor of this action. Sets the <see cref="CityName"/> to name the city accordingly.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which the city should be added.</param>
        /// <param name="cityName">The name to be assigned to the created city.</param>
        public AddCityNetAction(string tableID, string cityName) : base(tableID)
        {
            CityName = cityName;
        }

        /// <summary>
        /// Adds the city of type <see cref="CityType"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
        }
    }
}
