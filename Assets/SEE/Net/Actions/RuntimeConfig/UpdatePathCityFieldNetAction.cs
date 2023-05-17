using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a file picker was changed. 
    /// </summary>
    public class UpdatePathCityFieldNetAction : AbstractNetAction
    {
        /// <summary>
        /// City index
        /// </summary>
        public int CityIndex;

        /// <summary>
        /// Widget path
        /// </summary>
        public string WidgetPath;
        
        /// <summary>
        /// Whether the path is absolute or relative.
        /// </summary>
        public bool IsAbsolute;
        
        /// <summary>
        /// The changed value
        /// </summary>
        public string Value;

        /// <summary>
        /// Does nothing on the server.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Triggers 'SyncPath' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncPath?.Invoke(WidgetPath, Value, IsAbsolute);
            }
        }
    }
}