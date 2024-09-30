namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action to synchronize cities between different players.
    /// </summary>
    public abstract class UpdateCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// City index
        /// </summary>
        public int CityIndex;

        /// <summary>
        /// Widget path
        /// </summary>
        public string WidgetPath;
    }
}
