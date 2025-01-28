using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a file picker was changed.
    /// </summary>
    public class UpdatePathCityFieldNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// Whether the path is absolute or relative.
        /// </summary>
        public bool IsAbsolute;

        /// <summary>
        /// The changed value
        /// </summary>
        public string Value;

        /// <summary>
        /// Triggers 'SyncPath' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncPath?.Invoke(WidgetPath, Value, IsAbsolute);
        }
    }
}
