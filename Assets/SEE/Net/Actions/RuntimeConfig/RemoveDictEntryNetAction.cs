using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a dictionary entry was removed.
    /// </summary>
    public class RemoveDictEntryNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// The key to be removed.
        /// </summary>
        public string Key;

        /// <summary>
        /// Triggers 'SyncRemoveDictEntry' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncRemoveDictEntry?.Invoke(WidgetPath, Key);
        }
    }
}
