
using SEE.Game.City;

namespace SEE.Net
{
    /// <summary>
    /// Interface for persisting code city data.
    ///
    /// Specifies a contract for loading and saving code city data.
    /// This will store the actual absoulte state of the code city with the position of nodes and edges.
    /// </summary>
    public interface ICodeCityPersitance
    {
        /// <summary>
        /// Loads the code city data from <paramref name="snapshot"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void LoadFromSnapshot(SeeCitySnapshot snapshot);

        /// <summary>
        /// Saves the current code city data to a snapshot.
        /// </summary>
        /// <returns>The snapshot that should be saved</returns>
        public SeeCitySnapshot CreateSnapshot();

        public CityTypes GetCityType();
    }
}
