#nullable enable
using SEE.Game.City;

namespace SEE.Net
{
    /// <summary>
    /// Interface for persisting code city data.
    ///
    /// Specifies methods for loading and saving code city data.
    /// This will store the current configuration, the actual absolute state of the code city's graph and the current layout.
    /// </summary>
    public interface ICodeCityPersistence
    {
        /// <summary>
        /// Loads the code city based on <paramref name="snapshot"/>
        /// </summary>
        /// <param name="snapshot">The data from the snapshot to load.</param>
        public void LoadFromSnapshot(SEECitySnapshot snapshot);

        /// <summary>
        /// Creates a snapshot of the code city.
        /// </summary>
        /// <returns>The snapshot that waS be created. May be null when the city has no loaded graph</returns>
        public SEECitySnapshot? CreateSnapshot();

        /// <summary>
        /// Returns the <see cref="CityTypes"/> of the current city.
        /// </summary>
        /// <returns>The city type.</returns>
        public string GetCityName();
    }
}
