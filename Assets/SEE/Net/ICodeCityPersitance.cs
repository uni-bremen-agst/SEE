
namespace SEE.Net
{
    public interface ICodeCityPersitance
    {
        /// <summary>
        /// Loads the code city data from <paramref name="snapshot"/>
        /// </summary>
        /// <param name="snapshot"></param>
        void LoadData(SeeCitySnapshot snapshot);

        SeeCitySnapshot SaveData();
    }
}
