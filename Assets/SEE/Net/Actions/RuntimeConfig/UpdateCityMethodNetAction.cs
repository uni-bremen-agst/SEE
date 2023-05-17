using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a city method was called. 
    /// </summary>
    public class UpdateCityMethodNetAction : AbstractNetAction
    {
        /// <summary>
        /// City index
        /// </summary>
        public int CityIndex;
        
        /// <summary>
        /// The method name
        /// </summary>
        public string MethodName;

        /// <summary>
        /// Does nothing on the server.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Triggers 'SyncMethod' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncMethod?.Invoke(MethodName);
        }
    }
}