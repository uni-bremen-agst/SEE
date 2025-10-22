using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a city method was called to synchronize a code city's state.
    /// </summary>
    public class UpdateCityMethodNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// The method name to be called.
        /// </summary>
        public string MethodName;

        /// <summary>
        /// Triggers <see cref="RuntimeTabMenu.SyncMethod"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncMethod?.Invoke(MethodName);
        }
    }
}
