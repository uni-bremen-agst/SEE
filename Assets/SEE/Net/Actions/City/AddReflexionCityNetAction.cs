using Cysharp.Threading.Tasks;
using SEE.Game.City;

namespace SEE.Net.Actions.City
{
    /// <summary>
    /// Adds a reflexion city to all clients.
    /// </summary>
    public class AddReflexionCityNetAction : AddCityNetAction
    {
        /// <summary>
        /// The constructor of this action. It simply passes the required parameters to the superclass constructor.
        /// </summary>
        /// <param name="tableID">The unique name of the table to which the reflexion city should be added.</param>
        /// <param name="cityName">The name to be assigned to the created city.</param>
        public AddReflexionCityNetAction(string tableID, string cityName) : base(tableID, cityName) {}

        /// <summary>
        /// Adds the reflexion city to the <see cref="CityNetAction.City"/> identified by <see cref="TableID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            CityAdder.CreateReflexionCityAsync(City, CityName).Forget();
        }
    }
}
