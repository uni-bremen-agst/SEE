using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a float was changed. 
    /// </summary>
    public class UpdateFloatCityFieldNetAction : AbstractNetAction
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
        /// The changed value
        /// </summary>
        public float Value;

        /// <summary>
        /// Does nothing on the server.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Triggers 'SyncField' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncField?.Invoke(WidgetPath, Value);
        }
    }
}